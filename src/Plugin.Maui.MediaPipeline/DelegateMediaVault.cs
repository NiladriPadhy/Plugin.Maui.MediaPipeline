namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// <see cref="IMediaVault"/> that forwards to a caller-supplied function.
/// </summary>
public sealed class DelegateMediaVault : IMediaVault
{
    readonly Func<string, byte[], CancellationToken, Task> _write;

    /// <summary>
    /// Initializes the vault adapter.
    /// </summary>
    public DelegateMediaVault(Func<string, byte[], CancellationToken, Task> write)
    {
        _write = write ?? throw new ArgumentNullException(nameof(write));
    }

    /// <inheritdoc />
    public Task WriteAsync(string path, byte[] content, CancellationToken cancellationToken = default) =>
        _write(path, content, cancellationToken);
}
