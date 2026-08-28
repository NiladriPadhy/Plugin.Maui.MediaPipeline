namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Raised after a pipeline run completes.
/// </summary>
public sealed class MediaProcessedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes the event with the completed result.
    /// </summary>
    public MediaProcessedEventArgs(MediaResult result)
    {
        Result = result;
    }

    /// <summary>
    /// Gets the completed result.
    /// </summary>
    public MediaResult Result { get; }
}
