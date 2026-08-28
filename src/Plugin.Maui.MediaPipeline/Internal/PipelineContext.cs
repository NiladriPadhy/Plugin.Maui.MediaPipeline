namespace Plugin.Maui.MediaPipeline;

sealed class PipelineContext : IDisposable
{
    public required byte[] Bytes { get; set; }

    public SKBitmap? Bitmap { get; set; }

    public bool PixelsDirty { get; set; }

    public MediaFormat Format { get; set; }

    public int Quality { get; set; }

    public int SourceOrientation { get; set; } = 1;

    public bool OrientationApplied { get; set; }

    public bool SuppressAutoOrientation { get; set; }

    public bool AutoOrient { get; set; }

    public bool ExifRemoved { get; set; }

    public bool IsEncrypted { get; set; }

    public byte[]? EncryptionKey { get; set; }

    public required string FileName { get; set; }

    public MediaSourceKind SourceKind { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public void EnsureDecoded()
    {
        if (Bitmap is not null)
        {
            return;
        }

        if (IsEncrypted)
        {
            throw new MediaPipelineException(
                MediaPipelineError.InvalidOperation,
                "Pixel operations cannot run after Encrypt. Place resize, watermark, blur, and redact before Encrypt.");
        }

        Bitmap = ImageProcessor.Decode(Bytes);
        SourceOrientation = JpegExif.ReadOrientation(Bytes);
        RememberSize();
        PixelsDirty = false;
    }

    public void ApplyAutoOrientationIfNeeded()
    {
        if (SuppressAutoOrientation || OrientationApplied || !AutoOrient)
        {
            return;
        }

        EnsureDecoded();
        ApplyOrientationCore();
    }

    public void ApplyOrientationCore()
    {
        EnsureDecoded();
        if (SourceOrientation <= 1)
        {
            OrientationApplied = true;
            return;
        }

        var rotated = ImageProcessor.ApplyOrientation(Bitmap!, SourceOrientation);
        if (!ReferenceEquals(rotated, Bitmap))
        {
            Bitmap!.Dispose();
            Bitmap = rotated;
            PixelsDirty = true;
        }

        RememberSize();
        OrientationApplied = true;
    }

    public void ReplaceBitmap(SKBitmap next)
    {
        if (!ReferenceEquals(next, Bitmap))
        {
            Bitmap?.Dispose();
            Bitmap = next;
            PixelsDirty = true;
        }

        RememberSize();
    }

    public void RememberSize()
    {
        if (Bitmap is not null)
        {
            Width = Bitmap.Width;
            Height = Bitmap.Height;
        }
    }

    public byte[] FlushEncoded()
    {
        if (IsEncrypted)
        {
            return Bytes;
        }

        if (Bitmap is not null && PixelsDirty)
        {
            Bytes = ImageProcessor.Encode(Bitmap, Format, Quality);
            PixelsDirty = false;
            ExifRemoved = true;
        }

        return Bytes;
    }

    public void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
    }
}
