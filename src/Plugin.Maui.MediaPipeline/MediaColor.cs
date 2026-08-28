namespace Plugin.Maui.MediaPipeline;

/// <summary>
/// 8-bit sRGB color used by watermarks and redaction.
/// </summary>
public readonly struct MediaColor : IEquatable<MediaColor>
{
    /// <summary>
    /// Initializes a color.
    /// </summary>
    public MediaColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Opaque black.
    /// </summary>
    public static MediaColor Black { get; } = new(0, 0, 0);

    /// <summary>
    /// Opaque white.
    /// </summary>
    public static MediaColor White { get; } = new(255, 255, 255);

    /// <inheritdoc />
    public bool Equals(MediaColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MediaColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(MediaColor left, MediaColor right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(MediaColor left, MediaColor right) => !left.Equals(right);
}
