namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Named stage reported through <see cref="MediaPipelineProgressEventArgs"/>.
/// </summary>
public enum MediaPipelineStage
{
    /// <summary>
    /// Loading camera, gallery, file, or memory bytes.
    /// </summary>
    Capture = 0,

    /// <summary>
    /// Decoding pixels.
    /// </summary>
    Decode = 1,

    /// <summary>
    /// Scaling the image.
    /// </summary>
    Resize = 2,

    /// <summary>
    /// Re-encoding with a JPEG quality.
    /// </summary>
    Compress = 3,

    /// <summary>
    /// Stripping EXIF and comment segments.
    /// </summary>
    RemoveExif = 4,

    /// <summary>
    /// Applying EXIF orientation to pixels.
    /// </summary>
    Orientation = 5,

    /// <summary>
    /// Drawing a text or image watermark.
    /// </summary>
    Watermark = 6,

    /// <summary>
    /// Blurring a region.
    /// </summary>
    Blur = 7,

    /// <summary>
    /// Painting a solid redaction rectangle.
    /// </summary>
    Redact = 8,

    /// <summary>
    /// AES-256-GCM encryption.
    /// </summary>
    Encrypt = 9,

    /// <summary>
    /// Writing the result to disk.
    /// </summary>
    Save = 10,

    /// <summary>
    /// Sending the result to an uploader.
    /// </summary>
    Upload = 11,

    /// <summary>
    /// Pipeline finished.
    /// </summary>
    Complete = 12
}
