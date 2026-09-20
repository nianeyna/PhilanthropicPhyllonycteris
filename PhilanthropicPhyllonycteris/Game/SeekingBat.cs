using Sunless.Game.Utilities;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.Game;

/// <summary>
/// The art of the bat the game sends circling the ship while the zee-bat is out looking
/// (<c>BatSignalIndication</c>): a black bat seen from above, head up, with a white disc of light
/// under it.
/// </summary>
internal static class SeekingBat
{
    // BatSignalIndication.Prefabs.SeekingBat; that class is private and its constants are inlined where used.
    private const string Prefab = "Sea/BatSignal";

    /// <summary>The bat and the light under it. False if the prefab is gone or no longer built that way.</summary>
    public static bool TryGetSprites(out Sprite bat, out Sprite glow)
    {
        bat = null!;
        glow = null!;
        var prefab = PrefabHelper.Instance.Get(Prefab, supressWarning: true);
        if (prefab == null) return false;

        // Depth first, so the bat comes before the light that is its child.
        var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (renderers.Length < 2 || renderers[0].sprite == null || renderers[1].sprite == null) return false;
        bat = renderers[0].sprite;
        glow = renderers[1].sprite;
        return true;
    }
}
