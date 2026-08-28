namespace Plugin.Maui.MediaPipeline.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task FromFile_AndFromStream_RoundTrip()
    {
        var source = TestImages.Jpeg(60, 40);
        var path = Path.Combine(Path.GetTempPath(), $"mpip-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(path, source);

        try
        {
            var fromFile = await MediaPipeline.FromFile(path).Compress(80).ToBytesAsync();
            await using var stream = File.OpenRead(path);
            var fromStream = await MediaPipeline.FromStream(stream, "shot.jpg", leaveOpen: true).Compress(80).ToBytesAsync();

            Assert.Equal(MediaSourceKind.File, fromFile.SourceKind);
            Assert.Equal(MediaSourceKind.Stream, fromStream.SourceKind);
            Assert.Equal(60, fromFile.Width);
            Assert.Equal(40, fromStream.Height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FromCameraAsync_UsesInjectedCapture()
    {
        var photo = TestImages.Jpeg(320, 240);
        var pipeline = MediaPipeline.Create(new MediaPipelineOptions
        {
            Capture = new FakeCapture(photo)
        });

        var result = await pipeline
            .FromCamera()
            .Resize(160)
            .Compress(80)
            .RemoveExif()
            .ToBytesAsync();

        Assert.Equal(MediaSourceKind.Camera, result.SourceKind);
        Assert.Equal(160, result.Width);
        Assert.Equal(120, result.Height);
    }

    [Fact]
    public async Task SaveAsync_WritesFile()
    {
        var source = TestImages.Jpeg(80, 60);
        var path = Path.Combine(Path.GetTempPath(), $"mpip-save-{Guid.NewGuid():N}.jpg");

        try
        {
            var result = await MediaPipeline.FromBytes(source).Compress(70).SaveAsync(path);
            Assert.Equal(path, result.FilePath);
            Assert.True(File.Exists(path));
            Assert.Equal(result.ByteCount, new FileInfo(path).Length);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task SaveToVault_WritesDelegate()
    {
        var source = TestImages.Jpeg(64, 48);
        var vault = new RecordingVault();

        var result = await MediaPipeline
            .FromBytes(source)
            .RemoveExif()
            .SaveToVaultAsync(vault, "inspections/v1.jpg");

        Assert.Single(vault.Writes);
        Assert.Equal("inspections/v1.jpg", vault.Writes[0].Path);
        Assert.Equal(result.Data, vault.Writes[0].Content);
    }

    [Fact]
    public async Task UploadAsync_PostsMultipart()
    {
        var source = TestImages.Jpeg(48, 48);
        var handler = new StubHandler();
        using var client = new HttpClient(handler);
        var pipeline = MediaPipeline.Create(new MediaPipelineOptions { HttpClient = client });

        var result = await pipeline
            .FromBytes(source)
            .Compress(70)
            .UploadAsync(new Uri("https://api.example/upload"));

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.NotNull(result.Upload);
        Assert.Equal("https://files.example/inspections/1.jpg", result.Upload!.RemoteUrl);
        Assert.Equal(201, result.Upload.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_Delegate_ReceivesResult()
    {
        var source = TestImages.Jpeg(32, 32);
        MediaResult? seen = null;

        var result = await MediaPipeline
            .FromBytes(source)
            .UploadAsync((media, _) =>
            {
                seen = media;
                return Task.FromResult(new MediaUploadResult { SessionId = "session-1", RemoteUrl = "https://tus/1" });
            });

        Assert.NotNull(seen);
        Assert.Equal(seen!.ByteCount, result.ByteCount);
        Assert.Equal("session-1", result.Upload?.SessionId);
    }

    [Fact]
    public async Task FluentExample_MatchesRequestedApi()
    {
        var photo = TestImages.Jpeg(640, 360);
        var previous = MediaPipeline.Current;
        MediaPipeline.SetDefault(MediaPipeline.Create(new MediaPipelineOptions
        {
            Capture = new FakeCapture(photo)
        }));

        try
        {
            var result = await MediaPipeline
                .FromCameraAsync()
                .Resize(1920)
                .Compress(80)
                .RemoveExif()
                .BlurRegion(MediaRegion.Relative(0.1f, 0.8f, 0.3f, 0.15f))
                .SaveAsync();

            Assert.True(result.Width <= 1920);
            Assert.True(result.ExifRemoved);
            Assert.False(string.IsNullOrWhiteSpace(result.FilePath));
            if (result.FilePath is { } path && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        finally
        {
            MediaPipeline.SetDefault(previous);
        }
    }

    [Fact]
    public async Task EmptyBytes_Throws()
    {
        var error = await Assert.ThrowsAsync<MediaPipelineException>(() =>
            MediaPipeline.FromBytes([]).ToBytesAsync());
        Assert.Equal(MediaPipelineError.InvalidImage, error.Error);
    }

    [Fact]
    public void Compress_RejectsInvalidQuality()
    {
        var error = Assert.Throws<MediaPipelineException>(() =>
            MediaPipeline.FromBytes(TestImages.Jpeg(16, 16)).Compress(0));
        Assert.Equal(MediaPipelineError.InvalidOperation, error.Error);
    }

    [Fact]
    public async Task Camera_OnNetTarget_IsNotSupported()
    {
        var pipeline = MediaPipeline.Create(new MediaPipelineOptions { Capture = new MauiMediaCapture() });
        Assert.False(pipeline.IsCameraSupported);

        var error = await Assert.ThrowsAsync<MediaPipelineException>(() =>
            pipeline.FromCamera().ToBytesAsync());
        Assert.Equal(MediaPipelineError.NotSupported, error.Error);
    }

    [Fact]
    public async Task Progress_ReportsStages()
    {
        var stages = new List<MediaPipelineStage>();
        var source = TestImages.Jpeg(80, 60);

        await MediaPipeline
            .FromBytes(source)
            .Resize(40)
            .Compress(70)
            .RemoveExif()
            .OnProgress((stage, _) => stages.Add(stage))
            .ToBytesAsync();

        Assert.Contains(MediaPipelineStage.Capture, stages);
        Assert.Contains(MediaPipelineStage.Resize, stages);
        Assert.Contains(MediaPipelineStage.Complete, stages);
    }
}
