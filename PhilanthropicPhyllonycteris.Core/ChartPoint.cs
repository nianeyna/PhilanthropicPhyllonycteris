using System;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// A position on the chart, local to the chart's root object (a tile is 150 across, origin at the
/// map's bottom left). A separate type from <see cref="ZeePoint"/> so the two scales cannot be mixed up.
/// </summary>
public readonly struct ChartPoint : IEquatable<ChartPoint>
{
    public float X { get; }
    public float Y { get; }

    public ChartPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(ChartPoint other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is ChartPoint other && Equals(other);
    public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
    public override string ToString() => $"({X}, {Y})";

    public static bool operator ==(ChartPoint a, ChartPoint b) => a.Equals(b);
    public static bool operator !=(ChartPoint a, ChartPoint b) => !a.Equals(b);
}
