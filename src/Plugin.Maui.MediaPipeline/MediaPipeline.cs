namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Entry point for the media pipeline when dependency injection is not used.
/// </summary>
/// <example>
/// <code>
/// var result = await MediaPipeline
///     .FromCameraAsync()
///     .Resize(1920)
///     .Compress(80)
///     .RemoveExif()
///     .Watermark("ACME Insurance")
///     .BlurRegion(MediaRegion.Relative(0.08f, 0.78f, 0.35f, 0.14f))
///     .SaveAsync();
/// </code>
/// </example>
public static class MediaPipeline
{
    static IMediaPipeline? _current;

    /// <summary>
    /// Gets the shared <see cref="IMediaPipeline"/> instance.
    /// </summary>
    public static IMediaPipeline Current => _current ??= Create(new MediaPipelineOptions());

    /// <summary>
    /// Creates a pipeline factory using MAUI <c>MediaPicker</c> when running on iOS or Android.
    /// </summary>
    public static IMediaPipeline Create(MediaPipelineOptions? options = null) =>
        new MediaPipelineImplementation(options ?? new MediaPipelineOptions(), options?.Capture ?? new MauiMediaCapture());

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IMediaPipeline implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    /// <summary>
    /// Starts a pipeline that opens the camera when a terminal method runs.
    /// Capture is deferred so this method can sit in a fluent chain.
    /// </summary>
    public static IMediaPipelineBuilder FromCameraAsync(MediaCaptureOptions? options = null) =>
        Current.FromCamera(options);

    /// <summary>
    /// Starts a pipeline that opens the camera when a terminal method runs.
    /// </summary>
    public static IMediaPipelineBuilder FromCamera(MediaCaptureOptions? options = null) =>
        Current.FromCamera(options);

    /// <summary>
    /// Starts a pipeline that opens the photo library when a terminal method runs.
    /// </summary>
    public static IMediaPipelineBuilder FromGalleryAsync(MediaPickOptions? options = null) =>
        Current.FromGallery(options);

    /// <summary>
    /// Starts a pipeline that opens the photo library when a terminal method runs.
    /// </summary>
    public static IMediaPipelineBuilder FromGallery(MediaPickOptions? options = null) =>
        Current.FromGallery(options);

    /// <summary>
    /// Starts a pipeline from an existing image file.
    /// </summary>
    public static IMediaPipelineBuilder FromFile(string path) => Current.FromFile(path);

    /// <summary>
    /// Starts a pipeline from a readable stream.
    /// </summary>
    public static IMediaPipelineBuilder FromStream(Stream stream, string? fileName = null, bool leaveOpen = false) =>
        Current.FromStream(stream, fileName, leaveOpen);

    /// <summary>
    /// Starts a pipeline from image bytes already in memory.
    /// </summary>
    public static IMediaPipelineBuilder FromBytes(byte[] data, string? fileName = null) =>
        Current.FromBytes(data, fileName);

    /// <summary>
    /// Decrypts a payload produced by <see cref="IMediaPipelineBuilder.Encrypt(byte[])"/>.
    /// </summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key) =>
        MediaCrypto.Decrypt(payload, key);
}
