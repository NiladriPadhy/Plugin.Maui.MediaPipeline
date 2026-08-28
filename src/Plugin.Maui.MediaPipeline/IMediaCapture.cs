namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Camera and gallery source. The default implementation uses MAUI <c>MediaPicker</c>.
/// </summary>
public interface IMediaCapture
{
    /// <summary>
    /// Gets a value indicating whether this target can open the camera.
    /// </summary>
    bool IsCaptureSupported { get; }

    /// <summary>
    /// Gets a value indicating whether this target can open the photo picker.
    /// </summary>
    bool IsPickSupported { get; }

    /// <summary>
    /// Captures a still image from the camera.
    /// </summary>
    Task<CapturedMedia> CapturePhotoAsync(MediaCaptureOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks an existing photo from the gallery.
    /// </summary>
    Task<CapturedMedia> PickPhotoAsync(MediaPickOptions? options = null, CancellationToken cancellationToken = default);
}
