namespace Plugin.Maui.MediaPipeline;

abstract class MediaOrigin
{
    public abstract MediaSourceKind Kind { get; }

    public abstract string FileName { get; }

    public abstract Task<byte[]> LoadAsync(CancellationToken cancellationToken);
}

sealed class BytesOrigin : MediaOrigin
{
    readonly byte[] _data;

    public BytesOrigin(byte[] data, string? fileName)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The image buffer is empty.");
        }

        _data = data;
        FileName = string.IsNullOrWhiteSpace(fileName) ? "image.jpg" : fileName;
    }

    public override MediaSourceKind Kind => MediaSourceKind.Bytes;

    public override string FileName { get; }

    public override Task<byte[]> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_data);
    }
}

sealed class FileOrigin : MediaOrigin
{
    readonly string _path;

    public FileOrigin(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        FileName = Path.GetFileName(path);
    }

    public override MediaSourceKind Kind => MediaSourceKind.File;

    public override string FileName { get; }

    public override async Task<byte[]> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new MediaPipelineException(MediaPipelineError.Cancelled, "Reading the image file was cancelled.");
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.IoFailure, $"The file '{_path}' could not be read.", ex);
        }
    }
}

sealed class StreamOrigin : MediaOrigin
{
    readonly Stream _stream;
    readonly bool _leaveOpen;

    public StreamOrigin(Stream stream, string? fileName, bool leaveOpen)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!_stream.CanRead)
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The stream is not readable.");
        }

        _leaveOpen = leaveOpen;
        FileName = string.IsNullOrWhiteSpace(fileName) ? "image.jpg" : fileName;
    }

    public override MediaSourceKind Kind => MediaSourceKind.Stream;

    public override string FileName { get; }

    public override async Task<byte[]> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var buffer = new MemoryStream();
            await _stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (buffer.Length == 0)
            {
                throw new MediaPipelineException(MediaPipelineError.InvalidImage, "The stream produced no image bytes.");
            }

            return buffer.ToArray();
        }
        catch (MediaPipelineException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new MediaPipelineException(MediaPipelineError.Cancelled, "Reading the image stream was cancelled.");
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.IoFailure, "The image stream could not be read.", ex);
        }
        finally
        {
            if (!_leaveOpen)
            {
                await _stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}

sealed class CaptureOrigin : MediaOrigin
{
    readonly IMediaCapture _capture;
    readonly MediaCaptureOptions? _captureOptions;
    readonly MediaPickOptions? _pickOptions;
    readonly bool _gallery;
    string _fileName;

    public CaptureOrigin(IMediaCapture capture, bool gallery, MediaCaptureOptions? captureOptions, MediaPickOptions? pickOptions)
    {
        _capture = capture;
        _gallery = gallery;
        _captureOptions = captureOptions;
        _pickOptions = pickOptions;
        _fileName = gallery ? "gallery.jpg" : "camera.jpg";
    }

    public override MediaSourceKind Kind => _gallery ? MediaSourceKind.Gallery : MediaSourceKind.Camera;

    public override string FileName => _fileName;

    public override async Task<byte[]> LoadAsync(CancellationToken cancellationToken)
    {
        var captured = _gallery
            ? await _capture.PickPhotoAsync(_pickOptions, cancellationToken).ConfigureAwait(false)
            : await _capture.CapturePhotoAsync(_captureOptions, cancellationToken).ConfigureAwait(false);

        _fileName = captured.FileName;
        return captured.Data;
    }
}
