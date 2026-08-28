namespace Plugin.Maui.MediaPipeline.Tests;

public sealed class ImageProcessingTests
{
    [Fact]
    public async Task Resize_LimitsLongestSide()
    {
        var source = TestImages.Jpeg(800, 400);

        var result = await MediaPipeline
            .FromBytes(source)
            .Resize(200)
            .ToBytesAsync();

        Assert.Equal(200, result.Width);
        Assert.Equal(100, result.Height);
        Assert.Equal(MediaFormat.Jpeg, result.Format);
    }

    [Fact]
    public async Task Resize_DoesNotUpscaleByDefault()
    {
        var source = TestImages.Jpeg(120, 80);

        var result = await MediaPipeline
            .FromBytes(source)
            .Resize(1920)
            .ToBytesAsync();

        Assert.Equal(120, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public async Task ResizeBox_FitKeepsAspect()
    {
        var source = TestImages.Jpeg(400, 200);

        var result = await MediaPipeline
            .FromBytes(source)
            .Resize(100, 100, ResizeMode.Fit)
            .ToBytesAsync();

        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public async Task Compress_ReducesJpegSize()
    {
        var source = TestImages.Jpeg(640, 480, quality: 95);

        var high = await MediaPipeline.FromBytes(source).Compress(95).ToBytesAsync();
        var low = await MediaPipeline.FromBytes(source).Compress(35).ToBytesAsync();

        Assert.True(low.ByteCount < high.ByteCount);
    }

    [Fact]
    public async Task Watermark_ChangesPixels()
    {
        var source = TestImages.Jpeg(240, 180, new SKColor(20, 20, 20));

        var result = await MediaPipeline
            .FromBytes(source)
            .Watermark("CONFIDENTIAL", new WatermarkOptions
            {
                Position = WatermarkPosition.Center,
                Opacity = 1,
                FontSize = 28,
                Color = MediaColor.White
            })
            .ToBytesAsync();

        using var original = TestImages.Decode(source);
        using var processed = TestImages.Decode(result.Data);
        Assert.NotEqual(Sample(original, 120, 90), Sample(processed, 120, 90));
    }

    [Fact]
    public async Task RedactRegion_FillsBlack()
    {
        var source = TestImages.Jpeg(200, 120, new SKColor(200, 40, 40));

        var result = await MediaPipeline
            .FromBytes(source)
            .RedactRegion(MediaRegion.Pixels(10, 10, 40, 40), MediaColor.Black)
            .ToBytesAsync();

        using var processed = TestImages.Decode(result.Data);
        var pixel = processed.GetPixel(20, 20);
        Assert.True(pixel.Red < 40 && pixel.Green < 40 && pixel.Blue < 40);
    }

    [Fact]
    public async Task BlurRegion_ChangesTargetWithoutClearingWholeImage()
    {
        var source = TestImages.Jpeg(200, 120, new SKColor(10, 10, 10), new SKColor(250, 20, 20));

        var result = await MediaPipeline
            .FromBytes(source)
            .Format(MediaFormat.Png)
            .BlurRegion(MediaRegion.Relative(0, 0, 0.3f, 1), sigma: 8)
            .ToBytesAsync();

        using var original = TestImages.Decode(source);
        using var processed = TestImages.Decode(result.Data);
        Assert.NotEqual(Sample(original, 10, 60), Sample(processed, 10, 60));
        Assert.Equal(Sample(original, 180, 60), Sample(processed, 180, 60));
    }

    [Fact]
    public async Task MaxBytes_HonorsLimit()
    {
        var source = TestImages.Jpeg(800, 600, quality: 95);

        var result = await MediaPipeline
            .FromBytes(source)
            .MaxBytes(40_000)
            .ToBytesAsync();

        Assert.True(result.ByteCount <= 40_000);
    }

    [Fact]
    public async Task Format_WritesPng()
    {
        var source = TestImages.Jpeg(80, 60);

        var result = await MediaPipeline
            .FromBytes(source)
            .Format(MediaFormat.Png)
            .ToBytesAsync();

        Assert.Equal(MediaFormat.Png, result.Format);
        Assert.Equal("image/png", result.ContentType);
        Assert.True(result.Data[0] == 0x89 && result.Data[1] == (byte)'P');
    }

    static SKColor Sample(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y);
}
