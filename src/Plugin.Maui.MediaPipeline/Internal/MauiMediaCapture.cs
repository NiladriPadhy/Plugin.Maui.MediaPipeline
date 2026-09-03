#if ANDROID || IOS || MACCATALYST || WINDOWS
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;

namespace Plugin.Maui.MediaPipeline;

sealed class MauiMediaCapture : IMediaCapture
{
    public bool IsCaptureSupported
    {
        get
        {
            try
            {
                return MediaPicker.Default.IsCaptureSupported;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public bool IsPickSupported => true;

    public Task<CapturedMedia> CapturePhotoAsync(MediaCaptureOptions? options = null, CancellationToken cancellationToken = default) =>
        CaptureAsync(gallery: false, options?.Title, cancellationToken);

    public Task<CapturedMedia> PickPhotoAsync(MediaPickOptions? options = null, CancellationToken cancellationToken = default) =>
        CaptureAsync(gallery: true, options?.Title, cancellationToken);

    static async Task<CapturedMedia> CaptureAsync(bool gallery, string? title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await EnsurePermissionAsync(gallery, cancellationToken).ConfigureAwait(false);

            var file = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var pickerOptions = new MediaPickerOptions { Title = title, SelectionLimit = 1 };
                if (!gallery)
                {
                    return await MediaPicker.Default.CapturePhotoAsync(pickerOptions).ConfigureAwait(false);
                }

                var photos = await MediaPicker.Default.PickPhotosAsync(pickerOptions).ConfigureAwait(false);
                return photos.FirstOrDefault();
            }).ConfigureAwait(false);

            if (file is null)
            {
                throw new MediaPipelineException(
                    MediaPipelineError.CaptureCancelled,
                    gallery ? "The photo picker was cancelled." : "The camera capture was cancelled.");
            }

            await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            var name = string.IsNullOrWhiteSpace(file.FileName)
                ? gallery ? "gallery.jpg" : "camera.jpg"
                : file.FileName;
            return new CapturedMedia(buffer.ToArray(), name);
        }
        catch (MediaPipelineException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new MediaPipelineException(MediaPipelineError.Cancelled, "The capture was cancelled.");
        }
        catch (PermissionException ex)
        {
            throw new MediaPipelineException(MediaPipelineError.PermissionDenied, "Camera or photo permission was denied.", ex);
        }
        catch (FeatureNotSupportedException ex)
        {
            throw new MediaPipelineException(MediaPipelineError.NotSupported, "Camera or gallery is not supported on this device.", ex);
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.CaptureFailed, "The image could not be captured.", ex);
        }
    }

    static async Task EnsurePermissionAsync(bool gallery, CancellationToken cancellationToken)
    {
        if (gallery)
        {
            var photos = await Permissions.RequestAsync<Permissions.Photos>().WaitAsync(cancellationToken).ConfigureAwait(false);
            if (photos is PermissionStatus.Granted or PermissionStatus.Limited)
            {
                return;
            }

            // Android photo picker can succeed without a broad storage grant.
            return;
        }

        var camera = await Permissions.RequestAsync<Permissions.Camera>().WaitAsync(cancellationToken).ConfigureAwait(false);
        if (camera != PermissionStatus.Granted)
        {
            throw new MediaPipelineException(MediaPipelineError.PermissionDenied, "Camera permission was denied.");
        }
    }
}
#endif
