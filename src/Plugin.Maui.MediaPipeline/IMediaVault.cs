namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Sink that stores processed bytes. Wire this to FileVault without taking a package reference:
/// <c>new DelegateMediaVault((path, bytes, ct) => FileVault.Current.WriteAsync(path, bytes, cancellationToken: ct))</c>.
/// </summary>
public interface IMediaVault
{
    /// <summary>
    /// Writes processed bytes to a logical vault path.
    /// </summary>
    Task WriteAsync(string path, byte[] content, CancellationToken cancellationToken = default);
}
