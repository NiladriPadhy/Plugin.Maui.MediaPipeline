using Microsoft.Extensions.Logging;
using Plugin.Maui.MediaPipeline;

namespace Plugin.Maui.MediaPipeline.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMediaPipeline(options =>
            {
                options.DefaultJpegQuality = 80;
                options.CorrectOrientationByDefault = true;
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
