namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Factory for fluent media pipelines.
/// </summary>
public interface IMediaPipeline
{
    /// <summary>
    /// Gets a value indicating whether camera capture is available on this target.
    /// </summary>
    bool IsCameraSupported { get; }

    /// <summary>
    /// Gets a value indicating whether gallery pick is available on this target.
    /// </summary>
    bool IsGallerySupported { get; }

    /// <summary>
    /// Raised as each stage starts.
    /// </summary>
    event EventHandler<MediaPipelineProgressEventArgs>? Progress;

    /// <summary>
    /// Raised after a terminal method finishes.
    /// </summary>
    event EventHandler<MediaProcessedEventArgs>? Completed;

    /// <summary>
    /// Starts a pipeline that opens the camera when a terminal method runs.
    /// </summary>
    IMediaPipelineBuilder FromCamera(MediaCaptureOptions? options = null);

    /// <summary>
    /// Starts a pipeline that opens the photo library when a terminal method runs.
    /// </summary>
    IMediaPipelineBuilder FromGallery(MediaPickOptions? options = null);

    /// <summary>
    /// Starts a pipeline from an existing image file.
    /// </summary>
    IMediaPipelineBuilder FromFile(string path);

    /// <summary>
    /// Starts a pipeline from a readable stream. The stream is read when a terminal method runs.
    /// </summary>
    IMediaPipelineBuilder FromStream(Stream stream, string? fileName = null, bool leaveOpen = false);

    /// <summary>
    /// Starts a pipeline from image bytes already in memory.
    /// </summary>
    IMediaPipelineBuilder FromBytes(byte[] data, string? fileName = null);
}
