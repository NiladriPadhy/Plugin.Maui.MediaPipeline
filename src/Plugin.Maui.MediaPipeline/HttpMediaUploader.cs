namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Uploads processed media as <c>multipart/form-data</c>.
/// </summary>
public sealed class HttpMediaUploader : IMediaUploader, IDisposable
{
    readonly HttpClient _client;
    readonly Uri _destination;
    readonly string _fieldName;
    readonly bool _ownsClient;

    /// <summary>
    /// Initializes an uploader that posts to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">POST URL.</param>
    /// <param name="client">Optional shared client. When omitted, a private client is created and disposed with this instance.</param>
    /// <param name="fieldName">Form field name. Default is <c>file</c>.</param>
    public HttpMediaUploader(Uri destination, HttpClient? client = null, string fieldName = "file")
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        _destination = destination;
        _fieldName = fieldName;
        _ownsClient = client is null;
        _client = client ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<MediaUploadResult> UploadAsync(MediaResult media, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);

        try
        {
            using var content = new MultipartFormDataContent();
            var part = new ByteArrayContent(media.Data);
            part.Headers.ContentType = new MediaTypeHeaderValue(media.IsEncrypted ? "application/octet-stream" : media.ContentType);
            content.Add(part, _fieldName, media.FileName);

            using var response = await _client.PostAsync(_destination, content, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new MediaPipelineException(
                    MediaPipelineError.UploadFailed,
                    $"Upload failed with {(int)response.StatusCode}: {body}");
            }

            return new MediaUploadResult
            {
                RemoteUrl = response.Headers.Location?.ToString() ?? _destination.ToString(),
                StatusCode = (int)response.StatusCode
            };
        }
        catch (MediaPipelineException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new MediaPipelineException(MediaPipelineError.Cancelled, "The upload was cancelled.");
        }
        catch (Exception ex)
        {
            throw new MediaPipelineException(MediaPipelineError.UploadFailed, "The media could not be uploaded.", ex);
        }
    }

    /// <summary>
    /// Disposes the private <see cref="HttpClient"/> when this instance created it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}
