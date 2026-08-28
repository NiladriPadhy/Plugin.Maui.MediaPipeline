namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// <see cref="IMediaUploader"/> that forwards to a caller-supplied function.
/// </summary>
public sealed class DelegateMediaUploader : IMediaUploader
{
    readonly Func<MediaResult, CancellationToken, Task<MediaUploadResult>> _upload;

    /// <summary>
    /// Initializes the uploader adapter.
    /// </summary>
    public DelegateMediaUploader(Func<MediaResult, CancellationToken, Task<MediaUploadResult>> upload)
    {
        _upload = upload ?? throw new ArgumentNullException(nameof(upload));
    }

    /// <inheritdoc />
    public Task<MediaUploadResult> UploadAsync(MediaResult media, CancellationToken cancellationToken = default) =>
        _upload(media, cancellationToken);
}
