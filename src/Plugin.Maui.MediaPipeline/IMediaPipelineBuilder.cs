namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Fluent image pipeline. Steps run in the order they are added when a terminal method is awaited.
/// </summary>
public interface IMediaPipelineBuilder
{
    /// <summary>
    /// Scales the image so the longest side is at most <paramref name="maxDimension"/> pixels.
    /// </summary>
    IMediaPipelineBuilder Resize(int maxDimension);

    /// <summary>
    /// Scales the image to a target box.
    /// </summary>
    IMediaPipelineBuilder Resize(int width, int height, ResizeMode mode = ResizeMode.Fit);

    /// <summary>
    /// Re-encodes as JPEG with <paramref name="quality"/> from 1 to 100.
    /// </summary>
    IMediaPipelineBuilder Compress(int quality);

    /// <summary>
    /// Sets the output container. JPEG is the default.
    /// </summary>
    IMediaPipelineBuilder Format(MediaFormat format);

    /// <summary>
    /// Removes EXIF, GPS, and JPEG comment metadata. Pixel steps already re-encode without EXIF.
    /// </summary>
    IMediaPipelineBuilder RemoveExif();

    /// <summary>
    /// Rotates and mirrors pixels using the EXIF orientation tag.
    /// </summary>
    IMediaPipelineBuilder CorrectOrientation();

    /// <summary>
    /// Keeps the stored pixel orientation even when <see cref="MediaPipelineOptions.CorrectOrientationByDefault"/> is true.
    /// </summary>
    IMediaPipelineBuilder KeepOriginalOrientation();

    /// <summary>
    /// Draws a text watermark.
    /// </summary>
    IMediaPipelineBuilder Watermark(string text, WatermarkOptions? options = null);

    /// <summary>
    /// Draws an image watermark (typically a PNG logo).
    /// </summary>
    IMediaPipelineBuilder Watermark(byte[] image, WatermarkOptions? options = null);

    /// <summary>
    /// Blurs a rectangle. Use <see cref="MediaRegion.Relative"/> for plate / face regions that scale with the photo.
    /// </summary>
    IMediaPipelineBuilder BlurRegion(MediaRegion region, float sigma = 12);

    /// <summary>
    /// Fills a rectangle with a solid color (default black).
    /// </summary>
    IMediaPipelineBuilder RedactRegion(MediaRegion region, MediaColor? color = null);

    /// <summary>
    /// Re-encodes and downscales until the file is at most <paramref name="maxBytes"/> long.
    /// </summary>
    IMediaPipelineBuilder MaxBytes(int maxBytes);

    /// <summary>
    /// Encrypts the encoded bytes with AES-256-GCM. Generates a key when <paramref name="key"/> is null.
    /// </summary>
    IMediaPipelineBuilder Encrypt(byte[]? key = null);

    /// <summary>
    /// Encrypts the encoded bytes with AES-256-GCM.
    /// </summary>
    IMediaPipelineBuilder Encrypt(MediaEncryptionOptions options);

    /// <summary>
    /// Receives stage progress for this run.
    /// </summary>
    IMediaPipelineBuilder OnProgress(Action<MediaPipelineStage, double> handler);

    /// <summary>
    /// Encodes (and optionally encrypts) and writes a file. A unique path is created when <paramref name="path"/> is omitted.
    /// </summary>
    Task<MediaResult> SaveAsync(string? path = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes and returns the bytes without requiring a file path.
    /// </summary>
    Task<MediaResult> ToBytesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes and writes to <paramref name="vault"/>.
    /// </summary>
    Task<MediaResult> SaveToVaultAsync(IMediaVault vault, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes and writes using a FileVault-style callback.
    /// </summary>
    Task<MediaResult> SaveToVaultAsync(string path, Func<string, byte[], CancellationToken, Task> write, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes, writes a temp file when needed, and uploads.
    /// </summary>
    Task<MediaResult> UploadAsync(IMediaUploader uploader, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes and POSTs the file as multipart to <paramref name="destination"/>.
    /// </summary>
    Task<MediaResult> UploadAsync(Uri destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes and uploads with a SmartUpload-style callback.
    /// </summary>
    Task<MediaResult> UploadAsync(Func<MediaResult, CancellationToken, Task<MediaUploadResult>> upload, CancellationToken cancellationToken = default);
}
