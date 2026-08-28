namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Sink that uploads processed media. Use <see cref="HttpMediaUploader"/> or a delegate that calls SmartUpload.
/// </summary>
public interface IMediaUploader
{
    /// <summary>
    /// Uploads the completed result.
    /// </summary>
    Task<MediaUploadResult> UploadAsync(MediaResult media, CancellationToken cancellationToken = default);
}
