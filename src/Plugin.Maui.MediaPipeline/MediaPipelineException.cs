namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Thrown when a pipeline operation cannot be completed.
/// </summary>
public sealed class MediaPipelineException : Exception
{
    /// <summary>
    /// Initializes a new exception with an error code and message.
    /// </summary>
    public MediaPipelineException(MediaPipelineError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the classified error.
    /// </summary>
    public MediaPipelineError Error { get; }
}
