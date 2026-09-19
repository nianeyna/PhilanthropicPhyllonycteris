using HarmonyLib;
using PhilanthropicPhyllonycteris.Game;
using Sunless.Game.Data;
using Sunless.Game.Entities.Geography;

namespace PhilanthropicPhyllonycteris.Patches;

/// <summary>
/// Every new zee comes from here: a new game, or an heir who did not inherit the chart (one who
/// did reuses the dead captain's config and never gets here). Markers are keyed by layout, so
/// without this an heir who rolled a past captain's layout would find that captain's marker.
/// </summary>
[HarmonyPatch(typeof(CharacterRepository), nameof(CharacterRepository.GenerateTileConfig))]
internal static class FreshZeePatch
{
    private static void Postfix(SerializableTileConfig __result)
    {
        if (__result?.Tiles == null) return;
        MarkerStore.Instance.ForgetFreshMap(MapAccess.Fingerprint(__result));
    }
}
