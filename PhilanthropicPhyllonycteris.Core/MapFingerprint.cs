using System.Collections.Generic;
using System.Text;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// Names a map by its layout, so a marker follows the map rather than a save file: a captain's
/// autosave and named saves share it, as does an heir who inherits the chart. 64-bit FNV-1a over
/// the width and height (four little-endian bytes each), then each tile name in order as UTF-8
/// followed by a zero byte. Changing any of that orphans every stored marker.
/// </summary>
public static class MapFingerprint
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static string Compute(int width, int height, IEnumerable<string> tileNames)
    {
        var hash = OffsetBasis;
        hash = Add(hash, width);
        hash = Add(hash, height);
        foreach (var name in tileNames)
        {
            foreach (var b in Encoding.UTF8.GetBytes(name ?? string.Empty)) hash = Add(hash, b);
            hash = Add(hash, (byte)0);
        }
        return hash.ToString("x16");
    }

    private static ulong Add(ulong hash, int value)
    {
        for (var shift = 0; shift < 32; shift += 8) hash = Add(hash, (byte)(value >> shift));
        return hash;
    }

    private static ulong Add(ulong hash, byte value) => unchecked((hash ^ value) * Prime);
}
