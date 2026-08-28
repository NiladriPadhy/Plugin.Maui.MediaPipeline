namespace Plugin.Maui.MediaPipeline;

sealed class MediaPipelineImplementation : IMediaPipeline
{
    readonly MediaPipelineOptions _options;
    readonly IMediaCapture _capture;

    public MediaPipelineImplementation(MediaPipelineOptions options, IMediaCapture capture)
    {
        _options = options;
        _capture = capture;
    }

    public bool IsCameraSupported => _capture.IsCaptureSupported;

    public bool IsGallerySupported => _capture.IsPickSupported;

    public event EventHandler<MediaPipelineProgressEventArgs>? Progress;

    public event EventHandler<MediaProcessedEventArgs>? Completed;

    public IMediaPipelineBuilder FromCamera(MediaCaptureOptions? options = null) =>
        new MediaPipelineBuilder(this, _options, new CaptureOrigin(_capture, gallery: false, options, null));

    public IMediaPipelineBuilder FromGallery(MediaPickOptions? options = null) =>
        new MediaPipelineBuilder(this, _options, new CaptureOrigin(_capture, gallery: true, null, options));

    public IMediaPipelineBuilder FromFile(string path) =>
        new MediaPipelineBuilder(this, _options, new FileOrigin(path));

    public IMediaPipelineBuilder FromStream(Stream stream, string? fileName = null, bool leaveOpen = false) =>
        new MediaPipelineBuilder(this, _options, new StreamOrigin(stream, fileName, leaveOpen));

    public IMediaPipelineBuilder FromBytes(byte[] data, string? fileName = null) =>
        new MediaPipelineBuilder(this, _options, new BytesOrigin(data, fileName));

    internal void RaiseProgress(MediaPipelineStage stage, double progress) =>
        Progress?.Invoke(this, new MediaPipelineProgressEventArgs(stage, progress));

    internal void RaiseCompleted(MediaResult result) =>
        Completed?.Invoke(this, new MediaProcessedEventArgs(result));
}
