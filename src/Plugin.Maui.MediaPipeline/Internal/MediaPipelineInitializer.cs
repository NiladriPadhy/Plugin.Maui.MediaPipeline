using Microsoft.Maui.Hosting;

namespace Plugin.Maui.MediaPipeline;

sealed class MediaPipelineInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var pipeline = services.GetService<IMediaPipeline>() ?? MediaPipeline.Current;
        MediaPipeline.SetDefault(pipeline);
    }
}
