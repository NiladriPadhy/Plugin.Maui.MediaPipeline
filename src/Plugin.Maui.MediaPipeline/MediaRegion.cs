namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// Rectangle used by blur and redaction. Values are pixels unless <see cref="IsNormalized"/> is <c>true</c>.
/// </summary>
public readonly struct MediaRegion
{
    /// <summary>
    /// Initializes a region.
    /// </summary>
    public MediaRegion(float x, float y, float width, float height, bool isNormalized = false)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be greater than zero.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsNormalized = isNormalized;
    }

    /// <summary>
    /// Gets the left edge.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the top edge.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the width.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets a value indicating whether coordinates are 0–1 fractions of the image size.
    /// </summary>
    public bool IsNormalized { get; }

    /// <summary>
    /// Pixel rectangle in image space.
    /// </summary>
    public static MediaRegion Pixels(float x, float y, float width, float height) =>
        new(x, y, width, height, isNormalized: false);

    /// <summary>
    /// Relative rectangle. Each value is a fraction of the decoded width or height.
    /// </summary>
    public static MediaRegion Relative(float x, float y, float width, float height) =>
        new(x, y, width, height, isNormalized: true);
}
