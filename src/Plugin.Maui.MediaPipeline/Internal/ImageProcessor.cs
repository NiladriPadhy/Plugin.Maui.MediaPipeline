namespace Plugin.Maui.MediaPipeline;

internal static class ImageProcessor
{
    public static SKBitmap Decode(byte[] data)
    {
        try
        {
            var bitmap = SKBitmap.Decode(data);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                throw new MediaPipelineException(MediaPipelineError.DecodeFailed, "The image could not be decoded.");
            }

            return bitmap;
        }
        catch (MediaPipelineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.DecodeFailed, "The image could not be decoded.", ex);
        }
    }

    public static byte[] Encode(SKBitmap bitmap, MediaFormat format, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);
        using var image = SKImage.FromBitmap(bitmap);
        var encodedFormat = format == MediaFormat.Png ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        using var data = image.Encode(encodedFormat, format == MediaFormat.Png ? 100 : quality);
        if (data is null)
        {
            throw new MediaPipelineException(MediaPipelineError.IoFailure, "The image could not be encoded.");
        }

        return data.ToArray();
    }

    public static SKBitmap ResizeMax(SKBitmap source, int maxDimension, bool allowUpscale)
    {
        if (maxDimension < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "The maximum dimension must be at least 1.");
        }

        var longest = Math.Max(source.Width, source.Height);
        if (longest <= maxDimension && !allowUpscale)
        {
            return source;
        }

        var scale = maxDimension / (float)longest;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        return Scale(source, width, height, ResizeMode.Stretch);
    }

    public static SKBitmap ResizeBox(SKBitmap source, int width, int height, ResizeMode mode)
    {
        if (width < 1 || height < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "Resize width and height must be at least 1.");
        }

        if (source.Width == width && source.Height == height && mode != ResizeMode.Fill)
        {
            return source;
        }

        return Scale(source, width, height, mode);
    }

    public static SKBitmap ApplyOrientation(SKBitmap source, int orientation)
    {
        if (orientation is < 2 or > 8)
        {
            return source;
        }

        var swap = orientation is 5 or 6 or 7 or 8;
        var width = swap ? source.Height : source.Width;
        var height = swap ? source.Width : source.Height;
        var dest = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(dest);
        canvas.Clear(SKColors.Transparent);

        switch (orientation)
        {
            case 2:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case 3:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case 4:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case 5:
                canvas.RotateDegrees(90);
                canvas.Translate(0, -height);
                canvas.Scale(-1, 1);
                canvas.Translate(-width, 0);
                break;
            case 6:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case 7:
                canvas.Translate(width, height);
                canvas.RotateDegrees(270);
                canvas.Scale(-1, 1);
                canvas.Translate(-source.Width, 0);
                break;
            case 8:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(source, 0, 0);
        return dest;
    }

    public static void DrawTextWatermark(SKBitmap bitmap, string text, WatermarkOptions options)
    {
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToSk(options.Color).WithAlpha(OpacityByte(options.Opacity))
        };
        using var font = new SKFont(SKTypeface.Default, Math.Max(8, options.FontSize));
        var width = font.MeasureText(text);
        var metrics = font.Metrics;
        var height = metrics.Descent - metrics.Ascent;
        var (x, y) = Anchor(bitmap.Width, bitmap.Height, width, height, options.Position, options.Margin);
        canvas.DrawText(text, x, y - metrics.Ascent, font, paint);
    }

    public static void DrawImageWatermark(SKBitmap bitmap, byte[] logo, WatermarkOptions options)
    {
        using var mark = Decode(logo);
        var targetWidth = Math.Max(1, (int)Math.Round(bitmap.Width * Math.Clamp(options.ImageScale, 0.02f, 1f)));
        var scale = targetWidth / (float)mark.Width;
        var targetHeight = Math.Max(1, (int)Math.Round(mark.Height * scale));
        var (x, y) = Anchor(bitmap.Width, bitmap.Height, targetWidth, targetHeight, options.Position, options.Margin);
        using var canvas = new SKCanvas(bitmap);
        using var image = SKImage.FromBitmap(mark);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha(OpacityByte(options.Opacity)) };
        canvas.DrawImage(image, SKRect.Create(x, y, targetWidth, targetHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
    }

    public static void Blur(SKBitmap bitmap, MediaRegion region, float sigma)
    {
        var rect = ToPixelRect(region, bitmap.Width, bitmap.Height);
        if (rect.Width < 2 || rect.Height < 2)
        {
            return;
        }

        using var subset = new SKBitmap();
        if (!bitmap.ExtractSubset(subset, rect))
        {
            return;
        }

        var info = new SKImageInfo(rect.Width, rect.Height, bitmap.ColorType, bitmap.AlphaType);
        using var surface = SKSurface.Create(info);
        if (surface is null)
        {
            return;
        }

        using var blur = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(sigma, sigma) };
        surface.Canvas.DrawBitmap(subset, 0, 0, blur);
        using var snapshot = surface.Snapshot();
        using var canvas = new SKCanvas(bitmap);
        canvas.DrawImage(snapshot, rect.Left, rect.Top);
    }

    public static void Redact(SKBitmap bitmap, MediaRegion region, MediaColor color)
    {
        var rect = ToPixelRect(region, bitmap.Width, bitmap.Height);
        if (rect.Width < 1 || rect.Height < 1)
        {
            return;
        }

        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = ToSk(color), Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(rect.Left, rect.Top, rect.Width, rect.Height), paint);
    }

    public static byte[] FitMaxBytes(SKBitmap bitmap, MediaFormat format, int quality, int maxBytes, out SKBitmap? replacement)
    {
        if (maxBytes < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "MaxBytes must be at least 1.");
        }

        replacement = null;
        var current = bitmap;
        var ownsCurrent = false;
        var currentQuality = format == MediaFormat.Png ? 100 : Math.Clamp(quality, 1, 100);

        try
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var encoded = Encode(current, format, currentQuality);
                if (encoded.Length <= maxBytes)
                {
                    if (ownsCurrent)
                    {
                        replacement = current;
                        ownsCurrent = false;
                    }

                    return encoded;
                }

                if (format == MediaFormat.Jpeg && currentQuality > 24)
                {
                    currentQuality = Math.Max(24, currentQuality - 12);
                    continue;
                }

                var nextWidth = Math.Max(1, (int)(current.Width * 0.85));
                var nextHeight = Math.Max(1, (int)(current.Height * 0.85));
                if (nextWidth == current.Width && nextHeight == current.Height)
                {
                    break;
                }

                var scaled = Scale(current, nextWidth, nextHeight, ResizeMode.Stretch);
                if (ownsCurrent)
                {
                    current.Dispose();
                }

                current = scaled;
                ownsCurrent = true;
            }

            var last = Encode(current, format, Math.Max(16, currentQuality));
            if (last.Length <= maxBytes)
            {
                if (ownsCurrent)
                {
                    replacement = current;
                    ownsCurrent = false;
                }

                return last;
            }
        }
        finally
        {
            if (ownsCurrent)
            {
                current.Dispose();
            }
        }

        throw new MediaPipelineException(
            MediaPipelineError.InvalidOperation,
            $"The image could not be reduced below {maxBytes} bytes.");
    }

    static SKBitmap Scale(SKBitmap source, int width, int height, ResizeMode mode)
    {
        var dest = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(dest);
        canvas.Clear(SKColors.Transparent);
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        using var image = SKImage.FromBitmap(source);

        if (mode == ResizeMode.Stretch)
        {
            canvas.DrawImage(image, SKRect.Create(0, 0, width, height), sampling);
            return dest;
        }

        var scale = mode == ResizeMode.Fit
            ? Math.Min(width / (float)source.Width, height / (float)source.Height)
            : Math.Max(width / (float)source.Width, height / (float)source.Height);
        var drawWidth = source.Width * scale;
        var drawHeight = source.Height * scale;
        var x = (width - drawWidth) / 2f;
        var y = (height - drawHeight) / 2f;
        canvas.DrawImage(image, SKRect.Create(x, y, drawWidth, drawHeight), sampling);
        return dest;
    }

    static SKRectI ToPixelRect(MediaRegion region, int width, int height)
    {
        float x = region.X, y = region.Y, w = region.Width, h = region.Height;
        if (region.IsNormalized)
        {
            x *= width;
            y *= height;
            w *= width;
            h *= height;
        }

        var left = (int)Math.Floor(Math.Clamp(x, 0, width));
        var top = (int)Math.Floor(Math.Clamp(y, 0, height));
        var right = (int)Math.Ceiling(Math.Clamp(x + w, 0, width));
        var bottom = (int)Math.Ceiling(Math.Clamp(y + h, 0, height));
        return SKRectI.Create(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    static (float X, float Y) Anchor(int canvasWidth, int canvasHeight, float width, float height, WatermarkPosition position, float margin)
    {
        var inset = Math.Max(0, margin);
        var x = position switch
        {
            WatermarkPosition.TopLeft or WatermarkPosition.MiddleLeft or WatermarkPosition.BottomLeft => inset,
            WatermarkPosition.TopCenter or WatermarkPosition.Center or WatermarkPosition.BottomCenter => (canvasWidth - width) / 2f,
            _ => canvasWidth - width - inset
        };
        var y = position switch
        {
            WatermarkPosition.TopLeft or WatermarkPosition.TopCenter or WatermarkPosition.TopRight => inset,
            WatermarkPosition.MiddleLeft or WatermarkPosition.Center or WatermarkPosition.MiddleRight => (canvasHeight - height) / 2f,
            _ => canvasHeight - height - inset
        };
        return (x, y);
    }

    static SKColor ToSk(MediaColor color) => new(color.R, color.G, color.B, color.A);

    static byte OpacityByte(float opacity) => (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255);
}
