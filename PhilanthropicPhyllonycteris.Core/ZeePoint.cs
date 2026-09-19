using System;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>A position at zee in world units (a tile is 1500 across). The marker is stored as one of these.</summary>
public readonly struct ZeePoint : IEquatable<ZeePoint>
{
    public float X { get; }
    public float Y { get; }

    public ZeePoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(ZeePoint other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is ZeePoint other && Equals(other);
    public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
    public override string ToString() => $"({X}, {Y})";

    public static bool operator ==(ZeePoint a, ZeePoint b) => a.Equals(b);
    public static bool operator !=(ZeePoint a, ZeePoint b) => !a.Equals(b);
}
