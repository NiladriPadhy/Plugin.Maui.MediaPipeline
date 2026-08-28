namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Output image container written by the pipeline.
/// </summary>
public enum MediaFormat
{
    /// <summary>
    /// JPEG. Default for camera capture and <see cref="IMediaPipelineBuilder.Compress"/>.
    /// </summary>
    Jpeg = 0,

    /// <summary>
    /// PNG. Lossless; compression quality is ignored.
    /// </summary>
    Png = 1
}
