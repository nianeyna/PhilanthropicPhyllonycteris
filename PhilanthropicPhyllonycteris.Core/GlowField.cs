using System;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// A glow that follows a shape's outline: full strength on the shape, easing to nothing
/// <c>reach</c> pixels from its nearest edge. Images are row-major arrays of 0 to 1.
/// </summary>
public static class GlowField
{
    private const float ShapeThreshold = 0.5f;

    // Stands in for infinity; real infinities turn into NaN in the parabola intersections below.
    private const float Far = 1e20f;

    /// <summary>
    /// The glow around the shape whose coverage (alpha) is given. The image is not grown, so the
    /// caller leaves <paramref name="reach"/> pixels of empty margin for the glow to fall into.
    /// </summary>
    public static float[] Around(float[] coverage, int width, int height, float reach)
    {
        if (coverage.Length != width * height) throw new ArgumentException("Coverage does not match the size given.", nameof(coverage));

        var squared = new float[coverage.Length];
        for (var i = 0; i < squared.Length; i++) squared[i] = coverage[i] >= ShapeThreshold ? 0f : Far;
        SquaredDistances(squared, width, height);

        var glow = new float[squared.Length];
        for (var i = 0; i < glow.Length; i++)
        {
            // Squared, so it meets nothing with no visible edge, as light does.
            var t = Math.Max(0f, 1f - (float)Math.Sqrt(squared[i]) / reach);
            glow[i] = t * t;
        }
        return glow;
    }

    /// <summary>
    /// Shrinks an image by a whole <paramref name="factor"/>, averaging each block; blocks hanging
    /// over the far edges count what lies beyond as empty.
    /// </summary>
    public static float[] Shrink(float[] image, int width, int height, int factor, out int shrunkWidth, out int shrunkHeight)
    {
        shrunkWidth = (width + factor - 1) / factor;
        shrunkHeight = (height + factor - 1) / factor;
        var shrunk = new float[shrunkWidth * shrunkHeight];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++) shrunk[y / factor * shrunkWidth + x / factor] += image[y * width + x];
        }
        for (var i = 0; i < shrunk.Length; i++) shrunk[i] /= factor * factor;
        return shrunk;
    }

    #region Distance transform

    // Felzenszwalb and Huttenlocher's exact Euclidean distance transform: the one-dimensional
    // transform down every column, then along every row. Takes 0 on the shape and Far elsewhere.
    private static void SquaredDistances(float[] grid, int width, int height)
    {
        var longest = Math.Max(width, height);
        var line = new float[longest];
        var result = new float[longest];
        var parabolas = new int[longest];
        var boundaries = new float[longest + 1];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++) line[y] = grid[y * width + x];
            Transform(line, height, result, parabolas, boundaries);
            for (var y = 0; y < height; y++) grid[y * width + x] = result[y];
        }
        for (var y = 0; y < height; y++)
        {
            Array.Copy(grid, y * width, line, 0, width);
            Transform(line, width, result, parabolas, boundaries);
            Array.Copy(result, 0, grid, y * width, width);
        }
    }

    // The lower envelope of the parabolas rooted at each f[q], read off at every position.
    private static void Transform(float[] f, int n, float[] result, int[] parabolas, float[] boundaries)
    {
        var k = 0;
        parabolas[0] = 0;
        boundaries[0] = -Far;
        boundaries[1] = Far;
        for (var q = 1; q < n; q++)
        {
            float s;
            while (true)
            {
                var p = parabolas[k];
                s = (f[q] + (float)q * q - (f[p] + (float)p * p)) / (2f * q - 2f * p);
                if (s > boundaries[k]) break;
                k--;
            }
            k++;
            parabolas[k] = q;
            boundaries[k] = s;
            boundaries[k + 1] = Far;
        }

        k = 0;
        for (var q = 0; q < n; q++)
        {
            while (boundaries[k + 1] < q) k++;
            var p = parabolas[k];
            result[q] = Math.Min(Far, (float)(q - p) * (q - p) + f[p]);
        }
    }

    #endregion
}
