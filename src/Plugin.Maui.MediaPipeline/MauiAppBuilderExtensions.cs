using Microsoft.Maui.Hosting;

namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// MAUI host registration for MediaPipeline.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IMediaPipeline"/> as a singleton.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMediaPipeline(options =>
    /// {
    ///     options.DefaultJpegQuality = 80;
    ///     options.CorrectOrientationByDefault = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMediaPipeline(this MauiAppBuilder builder, Action<MediaPipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new MediaPipelineOptions();
        configure?.Invoke(options);

        builder.Services.AddMediaPipeline(options);
        builder.Services.AddTransient<IMauiInitializeService, MediaPipelineInitializer>();
        return builder;
    }
}
