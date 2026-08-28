#if !ANDROID && !IOS
namespace Plugin.Maui.MediaPipeline;

sealed class MauiMediaCapture : IMediaCapture
{
    public bool IsCaptureSupported => false;

    public bool IsPickSupported => false;

    public Task<CapturedMedia> CapturePhotoAsync(MediaCaptureOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromException<CapturedMedia>(new MediaPipelineException(
            MediaPipelineError.NotSupported,
            "Camera capture is only available on Android and iOS."));

    public Task<CapturedMedia> PickPhotoAsync(MediaPickOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromException<CapturedMedia>(new MediaPipelineException(
            MediaPipelineError.NotSupported,
            "Gallery pick is only available on Android and iOS."));
}
#endif
