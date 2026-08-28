namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Progress for one pipeline stage.
/// </summary>
public sealed class MediaPipelineProgressEventArgs : EventArgs
{
    /// <summary>
    /// Initializes progress for a stage.
    /// </summary>
    public MediaPipelineProgressEventArgs(MediaPipelineStage stage, double progress)
    {
        Stage = stage;
        Progress = Math.Clamp(progress, 0, 1);
    }

    /// <summary>
    /// Gets the stage that just started or finished.
    /// </summary>
    public MediaPipelineStage Stage { get; }

    /// <summary>
    /// Gets overall completion from 0 to 1.
    /// </summary>
    public double Progress { get; }
}
