using System.Linq;
using PhilanthropicPhyllonycteris.Core;
using Xunit;

namespace PhilanthropicPhyllonycteris.Tests;

public class MapFingerprintTests
{
    private static readonly string[] Tiles = { "SouthernArchipelago", "ShepherdsWash", "Ünterzee" };

    [Fact]
    public void Algorithm_is_pinned()
    {
        // Worked out independently of the implementation. If this fails the algorithm changed,
        // and every marker already on a player's disk has lost its map.
        Assert.Equal("8732978c07cd2e15", MapFingerprint.Compute(6, 6, Tiles));
    }

    [Fact]
    public void Layout_changes_change_the_fingerprint()
    {
        var original = MapFingerprint.Compute(6, 6, Tiles);

        Assert.NotEqual(original, MapFingerprint.Compute(6, 6, Tiles.Reverse()));
        Assert.NotEqual(original, MapFingerprint.Compute(3, 12, Tiles));
        // Name boundaries count: "ab","c" is not "a","bc".
        Assert.NotEqual(MapFingerprint.Compute(1, 2, new[] { "ab", "c" }), MapFingerprint.Compute(1, 2, new[] { "a", "bc" }));
    }
}
