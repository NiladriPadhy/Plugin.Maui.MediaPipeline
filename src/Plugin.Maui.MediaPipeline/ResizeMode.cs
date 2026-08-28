namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// How <see cref="IMediaPipelineBuilder.Resize(int, int, ResizeMode)"/> maps to the target box.
/// </summary>
public enum ResizeMode
{
    /// <summary>
    /// Keep aspect ratio and fit inside the box. Default.
    /// </summary>
    Fit = 0,

    /// <summary>
    /// Keep aspect ratio, fill the box, and crop overflow.
    /// </summary>
    Fill = 1,

    /// <summary>
    /// Stretch to the exact width and height.
    /// </summary>
    Stretch = 2
}
