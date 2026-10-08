using System.Collections.Generic;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>A marker's icon on screen: its centre and half its width, in pixels.</summary>
public readonly struct HitCircle
{
    public float X { get; }
    public float Y { get; }
    public float Radius { get; }

    public HitCircle(float x, float y, float radius)
    {
        X = x;
        Y = y;
        Radius = radius;
    }
}

/// <summary>
/// Whether a click lands on a marker, and which. Compared in screen pixels, since the game keeps
/// chart icons the same size on screen at every zoom.
/// </summary>
public static class MarkerHitTest
{
    public const int None = -1;

    public static bool Hits(float clickX, float clickY, float markerX, float markerY, float radiusPixels)
    {
        float dx = clickX - markerX, dy = clickY - markerY;
        return dx * dx + dy * dy <= radiusPixels * radiusPixels;
    }

    /// <summary>The index of the marker under the click, the nearest centre when icons overlap, or <see cref="None"/>.</summary>
    public static int Nearest(float clickX, float clickY, IReadOnlyList<HitCircle> circles)
    {
        var nearest = None;
        var nearestDistance = float.MaxValue;
        for (var i = 0; i < circles.Count; i++)
        {
            var circle = circles[i];
            if (!Hits(clickX, clickY, circle.X, circle.Y, circle.Radius)) continue;
            float dx = clickX - circle.X, dy = clickY - circle.Y;
            var distance = dx * dx + dy * dy;
            if (distance >= nearestDistance) continue;
            nearest = i;
            nearestDistance = distance;
        }
        return nearest;
    }
}
