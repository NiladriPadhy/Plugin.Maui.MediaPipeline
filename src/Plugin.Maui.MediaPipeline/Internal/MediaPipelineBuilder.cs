using Microsoft.Maui.Storage;

namespace Plugin.Maui.MediaPipeline;

sealed class MediaPipelineBuilder : IMediaPipelineBuilder
{
    readonly MediaPipelineImplementation _pipeline;
    readonly MediaPipelineOptions _options;
    readonly MediaOrigin _origin;
    readonly List<PipelineStep> _steps = [];
    Action<MediaPipelineStage, double>? _progress;

    public MediaPipelineBuilder(MediaPipelineImplementation pipeline, MediaPipelineOptions options, MediaOrigin origin)
    {
        _pipeline = pipeline;
        _options = options;
        _origin = origin;
    }

    public IMediaPipelineBuilder Resize(int maxDimension)
    {
        if (maxDimension < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "Resize dimension must be at least 1.");
        }

        _steps.Add(new ResizeMaxStep(maxDimension));
        return this;
    }

    public IMediaPipelineBuilder Resize(int width, int height, ResizeMode mode = ResizeMode.Fit)
    {
        if (width < 1 || height < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "Resize width and height must be at least 1.");
        }

        _steps.Add(new ResizeBoxStep(width, height, mode));
        return this;
    }

    public IMediaPipelineBuilder Compress(int quality)
    {
        if (quality is < 1 or > 100)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "Compress quality must be between 1 and 100.");
        }

        _steps.Add(new CompressStep(quality));
        return this;
    }

    public IMediaPipelineBuilder Format(MediaFormat format)
    {
        _steps.Add(new FormatStep(format));
        return this;
    }

    public IMediaPipelineBuilder RemoveExif()
    {
        _steps.Add(new RemoveExifStep());
        return this;
    }

    public IMediaPipelineBuilder CorrectOrientation()
    {
        _steps.Add(new CorrectOrientationStep());
        return this;
    }

    public IMediaPipelineBuilder KeepOriginalOrientation()
    {
        _steps.Add(new KeepOrientationStep());
        return this;
    }

    public IMediaPipelineBuilder Watermark(string text, WatermarkOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _steps.Add(new TextWatermarkStep(text, options ?? new WatermarkOptions()));
        return this;
    }

    public IMediaPipelineBuilder Watermark(byte[] image, WatermarkOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length == 0)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The watermark image is empty.");
        }

        _steps.Add(new ImageWatermarkStep(image, options ?? new WatermarkOptions()));
        return this;
    }

    public IMediaPipelineBuilder BlurRegion(MediaRegion region, float sigma = 12)
    {
        if (sigma <= 0)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "Blur sigma must be greater than zero.");
        }

        _steps.Add(new BlurStep(region, sigma));
        return this;
    }

    public IMediaPipelineBuilder RedactRegion(MediaRegion region, MediaColor? color = null)
    {
        _steps.Add(new RedactStep(region, color ?? MediaColor.Black));
        return this;
    }

    public IMediaPipelineBuilder MaxBytes(int maxBytes)
    {
        if (maxBytes < 1)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "MaxBytes must be at least 1.");
        }

        _steps.Add(new MaxBytesStep(maxBytes));
        return this;
    }

    public IMediaPipelineBuilder Encrypt(byte[]? key = null)
    {
        if (key is { Length: not MediaCrypto.KeySize })
        {
            throw new MediaPipelineException(MediaPipelineError.EncryptionFailed, "The encryption key must be 256 bits.");
        }

        _steps.Add(new EncryptStep(key));
        return this;
    }

    public IMediaPipelineBuilder Encrypt(MediaEncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Encrypt(options.Key);
    }

    public IMediaPipelineBuilder OnProgress(Action<MediaPipelineStage, double> handler)
    {
        _progress = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    public Task<MediaResult> SaveAsync(string? path = null, CancellationToken cancellationToken = default) =>
        RunAsync(writeFile: true, path, vault: null, vaultPath: null, uploader: null, cancellationToken);

    public Task<MediaResult> ToBytesAsync(CancellationToken cancellationToken = default) =>
        RunAsync(writeFile: false, path: null, vault: null, vaultPath: null, uploader: null, cancellationToken);

    public Task<MediaResult> SaveToVaultAsync(IMediaVault vault, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return RunAsync(writeFile: false, path: null, vault, path, uploader: null, cancellationToken);
    }

    public Task<MediaResult> SaveToVaultAsync(string path, Func<string, byte[], CancellationToken, Task> write, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(write);
        return SaveToVaultAsync(new DelegateMediaVault(write), path, cancellationToken);
    }

    public Task<MediaResult> UploadAsync(IMediaUploader uploader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uploader);
        return RunAsync(writeFile: false, path: null, vault: null, vaultPath: null, uploader, cancellationToken);
    }

    public Task<MediaResult> UploadAsync(Uri destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var uploader = new HttpMediaUploader(destination, _options.HttpClient);
        return RunAsync(writeFile: false, path: null, vault: null, vaultPath: null, uploader, cancellationToken);
    }

    public Task<MediaResult> UploadAsync(Func<MediaResult, CancellationToken, Task<MediaUploadResult>> upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        return UploadAsync(new DelegateMediaUploader(upload), cancellationToken);
    }

    async Task<MediaResult> RunAsync(
        bool writeFile,
        string? path,
        IMediaVault? vault,
        string? vaultPath,
        IMediaUploader? uploader,
        CancellationToken cancellationToken)
    {
        Report(MediaPipelineStage.Capture, 0.04);
        var bytes = await _origin.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The source produced an empty image.");
        }

        using var context = new PipelineContext
        {
            Bytes = bytes,
            FileName = _origin.FileName,
            SourceKind = _origin.Kind,
            Format = _options.DefaultFormat,
            Quality = Math.Clamp(_options.DefaultJpegQuality, 1, 100),
            AutoOrient = _options.CorrectOrientationByDefault
        };
        if (JpegExif.TryReadSize(bytes, out var width, out var height))
        {
            context.Width = width;
            context.Height = height;
        }

        try
        {
            await Task.Run(() => ApplySteps(context, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new MediaPipelineException(MediaPipelineError.Cancelled, "The pipeline was cancelled.");
        }

        context.FlushEncoded();
        var result = ToResult(context, filePath: null, upload: null);

        if (writeFile)
        {
            Report(MediaPipelineStage.Save, 0.88);
            var output = ResolveOutputPath(path, result);
            try
            {
                var directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(output, result.Data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new MediaPipelineException(MediaPipelineError.Cancelled, "Saving the image was cancelled.");
            }
            catch (Exception ex)
            {
                throw new MediaPipelineException(MediaPipelineError.IoFailure, "The processed image could not be saved.", ex);
            }

            result = CloneWith(result, filePath: output, upload: null);
        }

        if (vault is not null && vaultPath is not null)
        {
            Report(MediaPipelineStage.Save, 0.9);
            await vault.WriteAsync(vaultPath, result.Data, cancellationToken).ConfigureAwait(false);
            result = CloneWith(result, filePath: vaultPath, upload: null);
        }

        if (uploader is not null)
        {
            Report(MediaPipelineStage.Upload, 0.94);
            var upload = await uploader.UploadAsync(result, cancellationToken).ConfigureAwait(false);
            result = CloneWith(result, result.FilePath, upload);
        }

        Report(MediaPipelineStage.Complete, 1);
        _pipeline.RaiseCompleted(result);
        return result;
    }

    void ApplySteps(PipelineContext context, CancellationToken cancellationToken)
    {
        if (_steps.Count == 0)
        {
            if (context.AutoOrient && !context.SuppressAutoOrientation)
            {
                context.EnsureDecoded();
                context.ApplyAutoOrientationIfNeeded();
                context.PixelsDirty = true;
            }

            return;
        }

        var index = 0;
        foreach (var step in _steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = 0.12 + ((index + 1) / (double)_steps.Count * 0.7);
            Report(step.Stage, progress);
            ApplyStep(context, step);
            index++;
        }
    }

    void ApplyStep(PipelineContext context, PipelineStep step)
    {
        switch (step)
        {
            case KeepOrientationStep:
                context.SuppressAutoOrientation = true;
                break;
            case CorrectOrientationStep:
                context.AutoOrient = true;
                context.SuppressAutoOrientation = false;
                context.ApplyOrientationCore();
                break;
            case FormatStep format:
                context.Format = format.Format;
                context.ApplyAutoOrientationIfNeeded();
                context.PixelsDirty = true;
                break;
            case CompressStep compress:
                context.Quality = compress.Quality;
                context.Format = MediaFormat.Jpeg;
                context.ApplyAutoOrientationIfNeeded();
                context.PixelsDirty = true;
                break;
            case ResizeMaxStep resizeMax:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                context.ReplaceBitmap(ImageProcessor.ResizeMax(context.Bitmap!, resizeMax.MaxDimension, _options.AllowUpscale));
                break;
            case ResizeBoxStep resizeBox:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                context.ReplaceBitmap(ImageProcessor.ResizeBox(context.Bitmap!, resizeBox.Width, resizeBox.Height, resizeBox.Mode));
                break;
            case RemoveExifStep:
                ApplyRemoveExif(context);
                break;
            case TextWatermarkStep text:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                ImageProcessor.DrawTextWatermark(context.Bitmap!, text.Text, text.Options);
                context.PixelsDirty = true;
                break;
            case ImageWatermarkStep image:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                ImageProcessor.DrawImageWatermark(context.Bitmap!, image.Image, image.Options);
                context.PixelsDirty = true;
                break;
            case BlurStep blur:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                ImageProcessor.Blur(context.Bitmap!, blur.Region, blur.Sigma);
                context.PixelsDirty = true;
                break;
            case RedactStep redact:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                ImageProcessor.Redact(context.Bitmap!, redact.Region, redact.Color);
                context.PixelsDirty = true;
                break;
            case MaxBytesStep max:
                context.ApplyAutoOrientationIfNeeded();
                context.EnsureDecoded();
                context.Bytes = ImageProcessor.FitMaxBytes(context.Bitmap!, context.Format, context.Quality, max.MaxBytes, out var replacement);
                if (replacement is not null)
                {
                    context.ReplaceBitmap(replacement);
                }

                context.PixelsDirty = false;
                context.ExifRemoved = true;
                break;
            case EncryptStep encrypt:
                context.ApplyAutoOrientationIfNeeded();
                context.FlushEncoded();
                if (context.Bitmap is not null && !context.ExifRemoved)
                {
                    context.PixelsDirty = true;
                    context.FlushEncoded();
                }

                var key = encrypt.Key ?? MediaCrypto.GenerateKey();
                context.RememberSize();
                context.Bytes = MediaCrypto.Encrypt(context.Bytes, key);
                context.IsEncrypted = true;
                context.EncryptionKey = key;
                context.Bitmap?.Dispose();
                context.Bitmap = null;
                context.PixelsDirty = false;
                break;
        }
    }

    static void ApplyRemoveExif(PipelineContext context)
    {
        if (context.IsEncrypted)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidOperation, "RemoveExif cannot run after Encrypt.");
        }

        if (context.Bitmap is not null)
        {
            context.ApplyAutoOrientationIfNeeded();
            context.PixelsDirty = true;
            context.ExifRemoved = true;
            return;
        }

        if (JpegExif.IsJpeg(context.Bytes))
        {
            context.Bytes = JpegExif.StripMetadata(context.Bytes);
            context.ExifRemoved = true;
            return;
        }

        context.EnsureDecoded();
        context.ApplyAutoOrientationIfNeeded();
        context.PixelsDirty = true;
        context.ExifRemoved = true;
    }

    MediaResult ToResult(PipelineContext context, string? filePath, MediaUploadResult? upload)
    {
        var data = context.FlushEncoded();
        var format = context.Format;
        var encrypted = context.IsEncrypted;
        var fileName = BuildFileName(context.FileName, format, encrypted);
        return new MediaResult
        {
            Data = data,
            FilePath = filePath,
            FileName = fileName,
            ContentType = encrypted ? "application/octet-stream" : format == MediaFormat.Png ? "image/png" : "image/jpeg",
            Width = context.Bitmap?.Width ?? context.Width,
            Height = context.Bitmap?.Height ?? context.Height,
            Format = format,
            SourceKind = context.SourceKind,
            ExifRemoved = context.ExifRemoved,
            OrientationApplied = context.OrientationApplied ? Math.Max(1, context.SourceOrientation) : 1,
            IsEncrypted = encrypted,
            EncryptionKey = context.EncryptionKey,
            Upload = upload
        };
    }

    static MediaResult CloneWith(MediaResult source, string? filePath, MediaUploadResult? upload) =>
        new()
        {
            Data = source.Data,
            FilePath = filePath,
            FileName = source.FileName,
            ContentType = source.ContentType,
            Width = source.Width,
            Height = source.Height,
            Format = source.Format,
            SourceKind = source.SourceKind,
            ExifRemoved = source.ExifRemoved,
            OrientationApplied = source.OrientationApplied,
            IsEncrypted = source.IsEncrypted,
            EncryptionKey = source.EncryptionKey,
            Upload = upload
        };

    string ResolveOutputPath(string? path, MediaResult result)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var root = _options.OutputDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            try
            {
                root = Path.Combine(FileSystem.AppDataDirectory, "MediaPipeline");
            }
            catch (Exception)
            {
                root = Path.Combine(Path.GetTempPath(), "MediaPipeline");
            }
        }

        Directory.CreateDirectory(root);
        return Path.Combine(root, $"{Path.GetFileNameWithoutExtension(result.FileName)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(result.FileName)}");
    }

    static string BuildFileName(string original, MediaFormat format, bool encrypted)
    {
        var stem = Path.GetFileNameWithoutExtension(original);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "media";
        }

        var ext = encrypted ? ".mpip" : format == MediaFormat.Png ? ".png" : ".jpg";
        return stem + ext;
    }

    void Report(MediaPipelineStage stage, double progress)
    {
        _progress?.Invoke(stage, progress);
        _pipeline.RaiseProgress(stage, progress);
    }
}
