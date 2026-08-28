namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Classifies a <see cref="MediaPipelineException"/>.
/// </summary>
public enum MediaPipelineError
{
    /// <summary>
    /// The operation is not valid in the current pipeline state.
    /// </summary>
    InvalidOperation = 0,

    /// <summary>
    /// Camera, gallery, or image processing is not available on this target.
    /// </summary>
    NotSupported = 1,

    /// <summary>
    /// The user cancelled camera or gallery capture.
    /// </summary>
    CaptureCancelled = 2,

    /// <summary>
    /// The camera or gallery could not produce an image.
    /// </summary>
    CaptureFailed = 3,

    /// <summary>
    /// The bytes could not be decoded as an image.
    /// </summary>
    DecodeFailed = 4,

    /// <summary>
    /// The input is not a usable image.
    /// </summary>
    InvalidImage = 5,

    /// <summary>
    /// Camera or photo library permission was denied.
    /// </summary>
    PermissionDenied = 6,

    /// <summary>
    /// A file could not be read or written.
    /// </summary>
    IoFailure = 7,

    /// <summary>
    /// Encryption failed.
    /// </summary>
    EncryptionFailed = 8,

    /// <summary>
    /// Ciphertext could not be authenticated or decrypted.
    /// </summary>
    DecryptionFailed = 9,

    /// <summary>
    /// The upload did not complete.
    /// </summary>
    UploadFailed = 10,

    /// <summary>
    /// The caller cancelled the pipeline.
    /// </summary>
    Cancelled = 11
}
