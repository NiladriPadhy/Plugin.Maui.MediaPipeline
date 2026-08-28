namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Placement and appearance for <see cref="IMediaPipelineBuilder.Watermark(string, WatermarkOptions?)"/>.
/// </summary>
public sealed class WatermarkOptions
{
    /// <summary>
    /// Gets or sets the anchor. Default is <see cref="WatermarkPosition.BottomRight"/>.
    /// </summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;

    /// <summary>
    /// Gets or sets text or image opacity, 0–1. Default is 0.55.
    /// </summary>
    public float Opacity { get; set; } = 0.55f;

    /// <summary>
    /// Gets or sets the text size in pixels. Default is 28.
    /// </summary>
    public float FontSize { get; set; } = 28;

    /// <summary>
    /// Gets or sets the text color. Default is white.
    /// </summary>
    public MediaColor Color { get; set; } = MediaColor.White;

    /// <summary>
    /// Gets or sets the inset from the anchored edge, in pixels. Default is 16.
    /// </summary>
    public float Margin { get; set; } = 16;

    /// <summary>
    /// Gets or sets the image-watermark width as a fraction of the photo width. Default is 0.18.
    /// </summary>
    public float ImageScale { get; set; } = 0.18f;
}
