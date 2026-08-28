using Plugin.Maui.MediaPipeline;

namespace Plugin.Maui.MediaPipeline.Sample;

public partial class MainPage : ContentPage
{
    readonly IMediaPipeline _pipeline;

    public MainPage(IMediaPipeline pipeline)
    {
        InitializeComponent();
        _pipeline = pipeline;
        _pipeline.Progress += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
            StatusLabel.Text = $"{e.Stage}… {e.Progress:P0}");
    }

    async void OnCameraClicked(object? sender, EventArgs e) =>
        await RunAsync("Camera", _pipeline.FromCamera());

    async void OnGalleryClicked(object? sender, EventArgs e) =>
        await RunAsync("Gallery", _pipeline.FromGallery());

    async Task RunAsync(string action, IMediaPipelineBuilder builder)
    {
        try
        {
            if (ResizeCheck.IsChecked)
            {
                builder = builder.Resize(1920);
            }

            if (CompressCheck.IsChecked)
            {
                builder = builder.Compress(80);
            }

            if (ExifCheck.IsChecked)
            {
                builder = builder.RemoveExif();
            }

            if (WatermarkCheck.IsChecked && !string.IsNullOrWhiteSpace(WatermarkEntry.Text))
            {
                builder = builder.Watermark(WatermarkEntry.Text, new WatermarkOptions
                {
                    Position = WatermarkPosition.BottomRight,
                    Opacity = 0.7f
                });
            }

            if (BlurCheck.IsChecked)
            {
                builder = builder.BlurRegion(MediaRegion.Relative(0.08f, 0.78f, 0.36f, 0.14f), sigma: 14);
            }

            if (EncryptCheck.IsChecked)
            {
                builder = builder.Encrypt();
            }

            var result = await builder.SaveAsync();

            if (result.IsEncrypted)
            {
                PreviewImage.Source = null;
            }
            else
            {
                PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(result.Data));
            }

            StatusLabel.Text =
                $"{result.Width}×{result.Height}  {result.ByteCount:N0} bytes  {result.ContentType}" +
                $"{Environment.NewLine}EXIF removed: {result.ExifRemoved}  encrypted: {result.IsEncrypted}" +
                $"{Environment.NewLine}{result.FilePath}";
        }
        catch (MediaPipelineException ex)
        {
            StatusLabel.Text = $"{action} failed ({ex.Error}): {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{action} failed: {ex.Message}";
        }
    }
}
