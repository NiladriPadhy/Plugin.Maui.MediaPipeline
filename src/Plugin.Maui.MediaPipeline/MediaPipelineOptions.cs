namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Defaults for <see cref="MediaPipeline"/> and <see cref="IMediaPipeline"/>.
/// </summary>
public sealed class MediaPipelineOptions
{
    /// <summary>
    /// Gets or sets the JPEG quality used when <see cref="IMediaPipelineBuilder.Compress"/> is omitted. Range 1–100. Default is 85.
    /// </summary>
    public int DefaultJpegQuality { get; set; } = 85;

    /// <summary>
    /// Gets or sets the encode format when the caller does not set one. Default is <see cref="MediaFormat.Jpeg"/>.
    /// </summary>
    public MediaFormat DefaultFormat { get; set; } = MediaFormat.Jpeg;

    /// <summary>
    /// Gets or sets the directory used by <see cref="IMediaPipelineBuilder.SaveAsync"/> when no path is supplied.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether EXIF orientation is applied when pixels are decoded. Default is <c>true</c>.
    /// </summary>
    public bool CorrectOrientationByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the longest side is allowed to grow during <see cref="IMediaPipelineBuilder.Resize(int)"/>. Default is <c>false</c>.
    /// </summary>
    public bool AllowUpscale { get; set; }

    /// <summary>
    /// Gets or sets a capture implementation. Tests and custom hosts inject this.
    /// </summary>
    public IMediaCapture? Capture { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="HttpClient"/> used by <see cref="HttpMediaUploader"/>.
    /// The pipeline does not dispose this instance.
    /// </summary>
    public HttpClient? HttpClient { get; set; }
}
