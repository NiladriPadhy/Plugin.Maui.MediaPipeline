namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// AES-256-GCM settings for <see cref="IMediaPipelineBuilder.Encrypt(MediaEncryptionOptions?)"/>.
/// </summary>
public sealed class MediaEncryptionOptions
{
    /// <summary>
    /// Gets or sets a 32-byte key. When null, the pipeline generates one and returns it on <see cref="MediaResult.EncryptionKey"/>.
    /// </summary>
    public byte[]? Key { get; set; }
}
