using System;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The mod's art, drawn in code so there are no asset files to ship. Shapes are white with a dark
/// rim, so a renderer's colour tints the shape and leaves the rim dark against any part of the chart.
/// </summary>
internal static class Sprites
{
    private const int Size = 64;
    private static readonly Color Rim = new Color(0.1f, 0.08f, 0.05f);

    private static Sprite? _cross;
    private static Sprite? _chevron;

    /// <summary>X marks the spot. <paramref name="unitsAcross"/> is its width in the units of whatever it is parented to.</summary>
    public static Sprite Cross(float unitsAcross)
    {
        if (_cross != null) return _cross;
        const float arm = 20f, halfStroke = 4.5f, rimWidth = 3f;
        _cross = Draw(unitsAcross, (x, y) => Math.Min(
            DistanceToSegment(x, y, -arm, -arm, arm, arm),
            DistanceToSegment(x, y, -arm, arm, arm, -arm)) - halfStroke, rimWidth);
        return _cross;
    }

    /// <summary>An arrowhead pointing along +x, so a rotation about z by the bearing aims it.</summary>
    public static Sprite Chevron(float unitsAcross)
    {
        if (_chevron != null) return _chevron;
        const float tip = 16f, tail = -12f, spread = 20f, halfStroke = 4.5f, rimWidth = 3f;
        _chevron = Draw(unitsAcross, (x, y) => Math.Min(
            DistanceToSegment(x, y, tip, 0f, tail, spread),
            DistanceToSegment(x, y, tip, 0f, tail, -spread)) - halfStroke, rimWidth);
        return _chevron;
    }

    #region Drawing

    // signedDistance is in pixels from the texture's centre: negative inside the shape.
    private static Sprite Draw(float unitsAcross, Func<float, float, float> signedDistance, float rimWidth)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color[Size * Size];
        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var d = signedDistance(px + 0.5f - Size / 2f, py + 0.5f - Size / 2f);
                // One pixel of blend at each edge: shape to rim at 0, rim to nothing at rimWidth.
                var colour = Color.Lerp(Color.white, Rim, Mathf.Clamp01(d + 0.5f));
                colour.a = 1f - Mathf.Clamp01(d - rimWidth + 0.5f);
                pixels[py * Size + px] = colour;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size / unitsAcross);
    }

    private static float DistanceToSegment(float x, float y, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax, aby = by - ay;
        var t = Mathf.Clamp01(((x - ax) * abx + (y - ay) * aby) / (abx * abx + aby * aby));
        float dx = x - (ax + t * abx), dy = y - (ay + t * aby);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    #endregion
}
