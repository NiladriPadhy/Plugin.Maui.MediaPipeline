namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Bytes returned by <see cref="IMediaCapture"/>.
/// </summary>
public sealed class CapturedMedia
{
    /// <summary>
    /// Initializes captured image bytes.
    /// </summary>
    public CapturedMedia(byte[] data, string fileName)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (data.Length == 0)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The capture produced an empty file.");
        }

        Data = data;
        FileName = fileName;
    }

    /// <summary>
    /// Gets the image bytes.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the original file name from the picker or camera.
    /// </summary>
    public string FileName { get; }
}
