namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Outcome of <see cref="IMediaUploader.UploadAsync"/>.
/// </summary>
public sealed class MediaUploadResult
{
    /// <summary>
    /// Gets or sets the remote URL or location returned by the server.
    /// </summary>
    public string? RemoteUrl { get; set; }

    /// <summary>
    /// Gets or sets an optional session id (for example a SmartUpload session).
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status when the uploader is HTTP-based.
    /// </summary>
    public int? StatusCode { get; set; }
}
