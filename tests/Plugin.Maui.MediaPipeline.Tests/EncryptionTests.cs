namespace Plugin.Maui.MediaPipeline.Tests;

public sealed class EncryptionTests
{
    [Fact]
    public async Task Encrypt_RoundTripsWithGeneratedKey()
    {
        var source = TestImages.Jpeg(64, 48);

        var result = await MediaPipeline
            .FromBytes(source)
            .Compress(80)
            .Encrypt()
            .ToBytesAsync();

        Assert.True(result.IsEncrypted);
        Assert.NotNull(result.EncryptionKey);
        Assert.Equal(32, result.EncryptionKey!.Length);
        Assert.Equal("application/octet-stream", result.ContentType);
        Assert.True(MediaCrypto.IsEnvelope(result.Data));

        var plain = MediaPipeline.Decrypt(result.Data, result.EncryptionKey);
        Assert.True(plain.Length > 0);
        Assert.Equal(0xFF, plain[0]);
    }

    [Fact]
    public async Task Encrypt_UsesSuppliedKey()
    {
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var source = TestImages.Jpeg(48, 48);

        var result = await MediaPipeline
            .FromBytes(source)
            .Encrypt(key)
            .ToBytesAsync();

        Assert.Equal(key, result.EncryptionKey);
        var plain = MediaPipeline.Decrypt(result.Data, key);
        using var bitmap = TestImages.Decode(plain);
        Assert.Equal(48, bitmap.Width);
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var key = new byte[32];
        key[0] = 7;
        var payload = MediaCrypto.Encrypt([1, 2, 3, 4], key);
        var other = new byte[32];
        other[0] = 9;

        var error = Assert.Throws<MediaPipelineException>(() => MediaPipeline.Decrypt(payload, other));
        Assert.Equal(MediaPipelineError.DecryptionFailed, error.Error);
    }

    [Fact]
    public async Task Encrypt_RejectsShortKey()
    {
        var source = TestImages.Jpeg(32, 32);

        var error = Assert.Throws<MediaPipelineException>(() =>
            MediaPipeline.FromBytes(source).Encrypt(new byte[16]));

        Assert.Equal(MediaPipelineError.EncryptionFailed, error.Error);
        await Task.CompletedTask;
    }
}
