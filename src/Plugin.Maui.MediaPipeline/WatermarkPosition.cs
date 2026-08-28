namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Anchor used to place a watermark.
/// </summary>
public enum WatermarkPosition
{
    /// <summary>
    /// Top-left corner.
    /// </summary>
    TopLeft = 0,

    /// <summary>
    /// Top-center edge.
    /// </summary>
    TopCenter = 1,

    /// <summary>
    /// Top-right corner.
    /// </summary>
    TopRight = 2,

    /// <summary>
    /// Middle-left edge.
    /// </summary>
    MiddleLeft = 3,

    /// <summary>
    /// Image center.
    /// </summary>
    Center = 4,

    /// <summary>
    /// Middle-right edge.
    /// </summary>
    MiddleRight = 5,

    /// <summary>
    /// Bottom-left corner.
    /// </summary>
    BottomLeft = 6,

    /// <summary>
    /// Bottom-center edge.
    /// </summary>
    BottomCenter = 7,

    /// <summary>
    /// Bottom-right corner.
    /// </summary>
    BottomRight = 8
}
