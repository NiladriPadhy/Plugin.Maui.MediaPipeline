namespace Plugin.Maui.MediaPipeline.Tests;

public sealed class ExifTests
{
    [Fact]
    public void StripMetadata_RemovesApp1()
    {
        var jpeg = TestImages.Jpeg(80, 60);
        var tagged = JpegExif.WithOrientation(jpeg, 6);

        Assert.True(JpegExif.HasExif(tagged));
        Assert.Equal(6, JpegExif.ReadOrientation(tagged));

        var stripped = JpegExif.StripMetadata(tagged);

        Assert.False(JpegExif.HasExif(stripped));
        Assert.Equal(1, JpegExif.ReadOrientation(stripped));
        Assert.True(JpegExif.IsJpeg(stripped));
    }

    [Fact]
    public async Task RemoveExif_DropsOrientationTag()
    {
        var tagged = JpegExif.WithOrientation(TestImages.Jpeg(80, 60), 3);

        var result = await MediaPipeline
            .FromBytes(tagged, "photo.jpg")
            .KeepOriginalOrientation()
            .RemoveExif()
            .ToBytesAsync();

        Assert.True(result.ExifRemoved);
        Assert.False(JpegExif.HasExif(result.Data));
    }

    [Fact]
    public async Task CorrectOrientation_RotatesPixels()
    {
        var source = TestImages.Jpeg(40, 20, new SKColor(0, 0, 200), new SKColor(220, 20, 20));
        var tagged = JpegExif.WithOrientation(source, 6);

        var result = await MediaPipeline
            .FromBytes(tagged)
            .KeepOriginalOrientation()
            .CorrectOrientation()
            .ToBytesAsync();

        Assert.Equal(20, result.Width);
        Assert.Equal(40, result.Height);
        Assert.Equal(6, result.OrientationApplied);

        using var processed = TestImages.Decode(result.Data);
        var top = processed.GetPixel(10, 4);
        Assert.True(top.Red > 150);
    }

    [Fact]
    public async Task DefaultPipeline_AppliesOrientationThenStripsExif()
    {
        var tagged = JpegExif.WithOrientation(TestImages.Jpeg(40, 20, accent: new SKColor(220, 20, 20)), 6);

        var result = await MediaPipeline
            .FromBytes(tagged)
            .Resize(40)
            .RemoveExif()
            .ToBytesAsync();

        Assert.Equal(20, result.Width);
        Assert.Equal(40, result.Height);
        Assert.False(JpegExif.HasExif(result.Data));
    }
}
