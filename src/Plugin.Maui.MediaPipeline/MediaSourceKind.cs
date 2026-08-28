namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Origin of the bytes that enter the pipeline.
/// </summary>
public enum MediaSourceKind
{
    /// <summary>
    /// Device camera.
    /// </summary>
    Camera = 0,

    /// <summary>
    /// Photo library / gallery picker.
    /// </summary>
    Gallery = 1,

    /// <summary>
    /// Existing file on disk.
    /// </summary>
    File = 2,

    /// <summary>
    /// Caller-supplied stream.
    /// </summary>
    Stream = 3,

    /// <summary>
    /// Caller-supplied byte array.
    /// </summary>
    Bytes = 4
}
