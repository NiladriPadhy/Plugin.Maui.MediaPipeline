namespace Plugin.Maui.MediaPipeline;

internal static class MediaCrypto
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const byte Version = 1;

    public static ReadOnlySpan<byte> Magic => "MPIP"u8;

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySize);

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);

        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using (var gcm = new AesGcm(key, TagSize))
            {
                gcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var output = new byte[Magic.Length + 1 + NonceSize + ciphertext.Length + TagSize];
            Magic.CopyTo(output);
            output[Magic.Length] = Version;
            nonce.CopyTo(output.AsSpan(Magic.Length + 1));
            ciphertext.CopyTo(output.AsSpan(Magic.Length + 1 + NonceSize));
            tag.CopyTo(output.AsSpan(Magic.Length + 1 + NonceSize + ciphertext.Length));
            return output;
        }
        catch (MediaPipelineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.EncryptionFailed, "The media could not be encrypted.", ex);
        }
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);

        var headerSize = Magic.Length + 1 + NonceSize + TagSize;
        if (payload.Length < headerSize)
        {
            throw new MediaPipelineException(MediaPipelineError.DecryptionFailed, "Encrypted payload is truncated.");
        }

        if (!payload[..Magic.Length].SequenceEqual(Magic))
        {
            throw new MediaPipelineException(MediaPipelineError.DecryptionFailed, "Encrypted payload has an unknown header.");
        }

        if (payload[Magic.Length] != Version)
        {
            throw new MediaPipelineException(MediaPipelineError.DecryptionFailed, $"Unsupported media payload version {payload[Magic.Length]}.");
        }

        var nonce = payload.Slice(Magic.Length + 1, NonceSize);
        var cipherLength = payload.Length - headerSize;
        var ciphertext = payload.Slice(Magic.Length + 1 + NonceSize, cipherLength);
        var tag = payload.Slice(Magic.Length + 1 + NonceSize + cipherLength, TagSize);
        var plaintext = new byte[cipherLength];

        try
        {
            using var gcm = new AesGcm(key, TagSize);
            gcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new MediaPipelineException(MediaPipelineError.DecryptionFailed, "The media could not be authenticated or decrypted.", ex);
        }

        return plaintext;
    }

    public static bool IsEnvelope(ReadOnlySpan<byte> payload) =>
        payload.Length >= Magic.Length + 1 && payload[..Magic.Length].SequenceEqual(Magic);

    static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
        {
            throw new MediaPipelineException(MediaPipelineError.EncryptionFailed, "The encryption key must be 256 bits.");
        }
    }
}
