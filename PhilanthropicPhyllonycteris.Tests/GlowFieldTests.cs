using System;
using PhilanthropicPhyllonycteris.Core;
using Xunit;

namespace PhilanthropicPhyllonycteris.Tests;

public class GlowFieldTests
{
    private const float Tolerance = 0.001f;

    private const int Width = 31;
    private const int Height = 21;
    private const float Reach = 8f;

    private static float[] Shape(params (int X, int Y)[] filled)
    {
        var coverage = new float[Width * Height];
        foreach (var (x, y) in filled) coverage[y * Width + x] = 1f;
        return coverage;
    }

    private static float At(float[] image, int x, int y) => image[y * Width + x];

    // What Around should give at a known distance from the shape.
    private static float Expected(float distance)
    {
        var t = Math.Max(0f, 1f - distance / Reach);
        return t * t;
    }

    #region Around

    [Fact]
    public void Glow_falls_off_with_true_distance_in_every_direction()
    {
        var glow = GlowField.Around(Shape((15, 10)), Width, Height, Reach);

        Assert.Equal(1f, At(glow, 15, 10), Tolerance);
        Assert.Equal(Expected(4f), At(glow, 19, 10), Tolerance);
        Assert.Equal(Expected(4f), At(glow, 15, 6), Tolerance);
        Assert.Equal(Expected(5f), At(glow, 12, 14), Tolerance);
        Assert.Equal(0f, At(glow, 15 + (int)Reach, 10), Tolerance);
        Assert.Equal(0f, At(glow, 0, 0), Tolerance);
    }

    [Fact]
    public void Glow_follows_the_nearest_part_of_the_shape()
    {
        // A bar along y = 10 with a spur rising from its left end.
        var glow = GlowField.Around(Shape((5, 10), (6, 10), (7, 10), (8, 10), (9, 10), (5, 11), (5, 12), (5, 13)), Width, Height, Reach);

        Assert.Equal(Expected(2f), At(glow, 8, 12), Tolerance);
        Assert.Equal(Expected(2f), At(glow, 7, 13), Tolerance);
        Assert.Equal(Expected(3f), At(glow, 12, 10), Tolerance);
    }

    [Fact]
    public void Faint_coverage_is_not_part_of_the_shape()
    {
        var coverage = Shape((15, 10));
        coverage[10 * Width + 20] = 0.2f;

        var glow = GlowField.Around(coverage, Width, Height, Reach);

        Assert.Equal(Expected(5f), At(glow, 20, 10), Tolerance);
    }

    [Fact]
    public void An_empty_image_has_no_glow()
    {
        var glow = GlowField.Around(Shape(), Width, Height, Reach);

        Assert.All(glow, value => Assert.Equal(0f, value));
    }

    #endregion

    #region Shrink

    [Fact]
    public void Shrinking_averages_blocks_and_counts_overhang_as_empty()
    {
        // 3 x 3 of full coverage, halved: the blocks on the far edges hang over by a pixel.
        var image = new float[9];
        for (var i = 0; i < image.Length; i++) image[i] = 1f;

        var shrunk = GlowField.Shrink(image, 3, 3, 2, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(2, height);
        Assert.Equal(new[] { 1f, 0.5f, 0.5f, 0.25f }, shrunk);
    }

    #endregion
}
