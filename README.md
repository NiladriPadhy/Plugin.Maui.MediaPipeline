# Plugin.Maui.MediaPipeline

A fluent **media processing pipeline** for **.NET MAUI** on **iOS** and **Android**.

Capture from the camera or gallery, then resize, compress, strip EXIF, correct orientation, watermark, blur or redact, encrypt, and hand the result to **FileVault** or **SmartUpload**.

```
Camera
  ↓
Resize
  ↓
Compress
  ↓
EXIF removal
  ↓
Orientation correction
  ↓
Watermark
  ↓
Blur / redact
  ↓
Encrypt
  ↓
Upload / vault / save
```

Built for field photos that leave the device: vehicle inspection, insurance, construction, healthcare, and document collection.

| Feature | What it does |
| --- | --- |
| **Capture** | Camera and gallery via MAUI `MediaPicker` |
| **Resize** | Longest-side or box fit / fill / stretch |
| **Compress** | JPEG quality 1–100, plus `MaxBytes` |
| **Privacy** | EXIF / GPS strip and orientation bake-in |
| **Overlay** | Text or image watermark |
| **Redaction** | Region blur or solid redact |
| **Encrypt** | AES-256-GCM envelope, decrypt helper |
| **Handoff** | File, `IMediaVault` (FileVault), `IMediaUploader` (SmartUpload / HTTP) |

## Install

```bash
dotnet add package Plugin.Maui.MediaPipeline
```

## Quick start

```csharp
using Plugin.Maui.MediaPipeline;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMediaPipeline(options =>
            {
                options.DefaultJpegQuality = 80;
                options.CorrectOrientationByDefault = true;
            });

        return builder.Build();
    }
}
```

```csharp
var result = await MediaPipeline
    .FromCameraAsync()
    .Resize(1920)
    .Compress(80)
    .RemoveExif()
    .Watermark("ACME Insurance")
    .BlurRegion(MediaRegion.Relative(0.08f, 0.78f, 0.36f, 0.14f))
    .SaveAsync();
```

`FromCameraAsync` is deferred: the camera opens when you await `SaveAsync`, `ToBytesAsync`, `SaveToVaultAsync`, or `UploadAsync`. Steps run in the order you add them.

Resolve `IMediaPipeline` or use `MediaPipeline.Current`.

## Sources

```csharp
MediaPipeline.FromCamera()
MediaPipeline.FromGallery()
MediaPipeline.FromFile(path)
MediaPipeline.FromStream(stream, "photo.jpg")
MediaPipeline.FromBytes(bytes)
```

## Processing

```csharp
.Resize(1920)                          // longest side, no upscale
.Resize(1280, 720, ResizeMode.Fill)    // crop to box
.Compress(80)                          // JPEG quality
.Format(MediaFormat.Png)
.MaxBytes(350_000)                     // quality then scale
.RemoveExif()
.CorrectOrientation()
.KeepOriginalOrientation()
.Watermark("CONFIDENTIAL")
.Watermark(logoBytes, new WatermarkOptions { Position = WatermarkPosition.TopLeft })
.BlurRegion(MediaRegion.Relative(0.1f, 0.8f, 0.3f, 0.15f), sigma: 14)
.RedactRegion(MediaRegion.Pixels(40, 40, 120, 40), MediaColor.Black)
.Encrypt()                             // generated 32-byte key on MediaResult.EncryptionKey
.Encrypt(existingKey)
```

Orientation is applied when pixels are decoded unless you call `KeepOriginalOrientation()`. `RemoveExif` after a pixel step is a re-encode; on a JPEG with no pixel steps it is a lossless APP1 strip.

## FileVault

The package does not reference FileVault. Pass a callback or `DelegateMediaVault`:

```csharp
var result = await MediaPipeline
    .FromCamera()
    .Resize(1920)
    .RemoveExif()
    .Encrypt()
    .SaveToVaultAsync($"inspections/{id}.jpg",
        (path, bytes, ct) => FileVault.Current.WriteAsync(path, bytes, cancellationToken: ct));

if (result.EncryptionKey is { } key)
{
    await FileVault.Current.WriteAsync($"inspections/{id}.key", key);
}
```

Store the generated key in FileVault or SecureStoragePlus. Decrypt later with `MediaPipeline.Decrypt(payload, key)`.

## SmartUpload

Save first (SmartUpload needs a file path), then enqueue:

```csharp
var result = await MediaPipeline
    .FromCamera()
    .Resize(1920)
    .Compress(80)
    .RemoveExif()
    .MaxBytes(512_000)
    .SaveAsync();

await SmartUpload.Current.EnqueueAsync(new UploadRequest
{
    FilePath = result.FilePath!,
    Endpoint = new Uri("https://api.example.com/uploads"),
    FileName = result.FileName,
    ContentType = result.ContentType
});
```

Or upload through the pipeline:

```csharp
await pipeline.UploadAsync(async (media, ct) =>
{
    var session = await SmartUpload.Current.EnqueueAsync(new UploadRequest
    {
        FilePath = media.FilePath ?? throw new InvalidOperationException("Save before upload."),
        Endpoint = uploadUrl
    }, ct);

    return new MediaUploadResult { SessionId = session.Id };
});
```

`UploadAsync(Uri)` POSTs `multipart/form-data` without SmartUpload.

## Platform setup

**Android** (`AndroidManifest.xml`):

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
```

**iOS** (`Info.plist`):

```xml
<key>NSCameraUsageDescription</key>
<string>This app captures inspection photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app reads photos for inspection upload.</string>
```

The pipeline requests camera permission before capture. Gallery pick uses the system photo picker when the OS provides one.

## Without the generic host

```csharp
var pipeline = MediaPipeline.Create(new MediaPipelineOptions
{
    DefaultJpegQuality = 75
});

var result = await pipeline.FromFile(path).Resize(1280).RemoveExif().ToBytesAsync();
```

`MediaPipeline.Create` does not replace `MediaPipeline.Current` unless you call `SetDefault`.

## Target frameworks

The package targets `net10.0`, `net10.0-android`, and `net10.0-ios`. Camera and gallery run on Android and iOS. The shared `net10.0` surface is for tests and in-memory processing.

## Pack from source

```bash
dotnet pack src/Plugin.Maui.MediaPipeline/Plugin.Maui.MediaPipeline.csproj -c Release -o artifacts
dotnet test tests/Plugin.Maui.MediaPipeline.Tests/Plugin.Maui.MediaPipeline.Tests.csproj
```

The `.nupkg` is written to `artifacts/Plugin.Maui.MediaPipeline.1.0.0.nupkg`.

## License

MIT

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
