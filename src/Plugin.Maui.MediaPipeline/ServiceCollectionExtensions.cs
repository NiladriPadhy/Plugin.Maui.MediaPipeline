namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Registers MediaPipeline services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IMediaPipeline"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddMediaPipeline(this IServiceCollection services, MediaPipelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IMediaCapture>(sp => options.Capture ?? new MauiMediaCapture());
        services.TryAddSingleton<IMediaPipeline>(sp =>
        {
            var pipeline = new MediaPipelineImplementation(options, sp.GetRequiredService<IMediaCapture>());
            MediaPipeline.SetDefault(pipeline);
            return pipeline;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMediaPipeline"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddMediaPipeline(this IServiceCollection services, Action<MediaPipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MediaPipelineOptions();
        configure?.Invoke(options);
        return services.AddMediaPipeline(options);
    }
}
