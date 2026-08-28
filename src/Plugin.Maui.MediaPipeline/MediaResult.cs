namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Output of a completed pipeline run.
/// </summary>
public sealed class MediaResult
{
    /// <summary>
    /// Gets the processed bytes. Encrypted when <see cref="IsEncrypted"/> is <c>true</c>.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets the file written by <see cref="IMediaPipelineBuilder.SaveAsync"/>, when one was created.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the suggested file name, including extension.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the MIME type of <see cref="Data"/> before encryption. Encrypted payloads use <c>application/octet-stream</c>.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the pixel width after orientation and resize. Zero when the payload is encrypted without a prior decode.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the pixel height after orientation and resize.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets <see cref="Data"/> length.
    /// </summary>
    public int ByteCount => Data.Length;

    /// <summary>
    /// Gets the container written when the image was encoded.
    /// </summary>
    public MediaFormat Format { get; init; }

    /// <summary>
    /// Gets the source that produced the original bytes.
    /// </summary>
    public MediaSourceKind SourceKind { get; init; }

    /// <summary>
    /// Gets a value indicating whether EXIF was stripped or discarded by re-encode.
    /// </summary>
    public bool ExifRemoved { get; init; }

    /// <summary>
    /// Gets the EXIF orientation that was applied, or 1 when none was needed.
    /// </summary>
    public int OrientationApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Data"/> is an AES-256-GCM envelope.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets the 32-byte key when the pipeline generated one. Callers must store this securely (for example in FileVault).
    /// </summary>
    public byte[]? EncryptionKey { get; init; }

    /// <summary>
    /// Gets upload details when a terminal upload ran.
    /// </summary>
    public MediaUploadResult? Upload { get; init; }
}
