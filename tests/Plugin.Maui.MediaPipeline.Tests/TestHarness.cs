namespace Plugin.Maui.MediaPipeline.Tests;

static class TestImages
{
    public static byte[] Jpeg(int width, int height, SKColor? background = null, SKColor? accent = null, int quality = 90)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background ?? new SKColor(70, 130, 180));
        if (accent is { } stripe)
        {
            using var paint = new SKPaint { Color = stripe, Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(0, 0, Math.Max(1, width / 4f), height), paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        Assert.NotNull(data);
        return data.ToArray();
    }

    public static byte[] Png(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        Assert.NotNull(data);
        return data.ToArray();
    }

    public static SKBitmap Decode(byte[] data)
    {
        var bitmap = SKBitmap.Decode(data);
        Assert.NotNull(bitmap);
        return bitmap;
    }
}

sealed class FakeCapture : IMediaCapture
{
    public FakeCapture(byte[] photo, string fileName = "camera.jpg")
    {
        Photo = photo;
        FileName = fileName;
    }

    public byte[] Photo { get; }

    public string FileName { get; }

    public bool IsCaptureSupported => true;

    public bool IsPickSupported => true;

    public Task<CapturedMedia> CapturePhotoAsync(MediaCaptureOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CapturedMedia(Photo, FileName));

    public Task<CapturedMedia> PickPhotoAsync(MediaPickOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CapturedMedia(Photo, "gallery.jpg"));
}

sealed class RecordingVault : IMediaVault
{
    public List<(string Path, byte[] Content)> Writes { get; } = [];

    public Task WriteAsync(string path, byte[] content, CancellationToken cancellationToken = default)
    {
        Writes.Add((path, content));
        return Task.CompletedTask;
    }
}

sealed class StubHandler : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    public HttpResponseMessage Response { get; set; } = new(System.Net.HttpStatusCode.Created)
    {
        Headers = { Location = new Uri("https://files.example/inspections/1.jpg") }
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        if (request.Content is not null)
        {
            _ = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        return Response;
    }
}
