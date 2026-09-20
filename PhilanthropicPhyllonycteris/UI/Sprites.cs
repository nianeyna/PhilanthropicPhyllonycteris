using System;
using System.Collections.Generic;
using PhilanthropicPhyllonycteris.Game;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The mod's art, drawn in code or borrowed from the game so there are no asset files to ship.
/// Shapes are white with a dark rim, so a renderer's colour tints the shape and leaves the rim dark
/// against any part of the chart.
/// </summary>
internal static class Sprites
{
    private const int Size = 64;
    private static readonly Color Rim = new Color(0.1f, 0.08f, 0.05f);

    private static Sprite? _bat;
    private static bool _batTried;
    private static Sprite? _cross;
    private static Sprite? _chevron;

    /// <summary>
    /// The game's zee-bat chart icon with its green disc bleached to white, so a renderer's colour
    /// decides the disc while the bat stays black. It keeps the game's size (5.78 chart units across).
    /// Null if the game's icon cannot be had, which is tried once.
    /// </summary>
    public static Sprite? Bat()
    {
        if (_batTried) return _bat;
        _batTried = true;
        try
        {
            var source = MapAccess.BatIconSprite;
            if (source != null) _bat = Bleach(source);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not copy the game's bat icon: {e.Message}");
        }
        if (_bat == null) Plugin.Log.LogWarning("The game's bat icon is not available, so the marker is a cross instead.");
        return _bat;
    }

    /// <summary>The fallback for <see cref="Bat"/>: X marks the spot. <paramref name="unitsAcross"/> is its width in the units of whatever it is parented to.</summary>
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

    #region Borrowing

    // Greyscale copy of a sprite, scaled so its commonest colour (an icon's disc) comes out white.
    private static Sprite Bleach(Sprite source)
    {
        var texture = CopyPixels(source);
        var pixels = texture.GetPixels32();

        var counts = new Dictionary<int, int>();
        var disc = 0f;
        var most = 0;
        foreach (var pixel in pixels)
        {
            if (pixel.a < 255) continue;
            var key = pixel.r << 16 | pixel.g << 8 | pixel.b;
            counts.TryGetValue(key, out var count);
            counts[key] = ++count;
            if (count <= most) continue;
            most = count;
            disc = ((Color)pixel).grayscale;
        }
        if (disc <= 0f) disc = 1f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var value = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(((Color)pixels[i]).grayscale / disc));
            pixels[i] = new Color32(value, value, value, pixels[i].a);
        }
        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        var size = new Vector2(texture.width, texture.height);
        return Sprite.Create(texture, new Rect(Vector2.zero, size), source.pivot / size, source.pixelsPerUnit);
    }

    // The game's textures are not readable from the CPU, so go by way of the GPU.
    private static Texture2D CopyPixels(Sprite source)
    {
        var atlas = source.texture;
        var rect = source.textureRect;
        var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        try
        {
            Graphics.Blit(atlas, target);
            RenderTexture.active = target;
            var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = atlas.filterMode,
            };
            copy.ReadPixels(rect, 0, 0);
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    #endregion

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
