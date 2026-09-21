using System;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>Where the indicator by the ship sits and which way it points.</summary>
public static class IndicatorMath
{
    public static float Distance(ZeePoint from, ZeePoint to)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// The direction to <paramref name="to"/> as a rotation about z in degrees: 0 along +x,
    /// anticlockwise positive, which is what a sprite drawn pointing right needs.
    /// </summary>
    public static float BearingDegrees(ZeePoint from, ZeePoint to) =>
        (float)(BearingRadians(from, to) * 180.0 / Math.PI);

    /// <summary>The point <paramref name="radius"/> from the ship on the line to the marker.</summary>
    public static ZeePoint RingPosition(ZeePoint ship, ZeePoint marker, float radius)
    {
        var radians = BearingRadians(ship, marker);
        return new ZeePoint(ship.X + (float)(Math.Cos(radians) * radius), ship.Y + (float)(Math.Sin(radians) * radius));
    }

    /// <summary>The indicator hides once the ship is near the marker; any closer and it would point back across the ship.</summary>
    public static bool IsVisible(ZeePoint ship, ZeePoint marker, float hideWithin) =>
        Distance(ship, marker) > hideWithin;

    private static double BearingRadians(ZeePoint from, ZeePoint to) =>
        Math.Atan2(to.Y - from.Y, to.X - from.X);
}
