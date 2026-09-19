using HarmonyLib;
using PhilanthropicPhyllonycteris.Game;
using PhilanthropicPhyllonycteris.UI;
using Sunless.Game.ApplicationProviders;

namespace PhilanthropicPhyllonycteris.Patches;

/// <summary>
/// The zee, and the chart with it, has just been rebuilt (on every load, not once per session),
/// so this is where the loaded map is named and its marker put back on the chart.
/// </summary>
[HarmonyPatch(typeof(MapProvider), nameof(MapProvider.Generate))]
internal static class ChartBuiltPatch
{
    private static void Postfix()
    {
        // Generate gives up early when the scene has no chart container.
        if (MapAccess.MapRoot == null) return;
        var character = GameProvider.Instance.CurrentCharacter;
        if (character?.TileConfig == null) return;
        MarkerStore.Instance.MapLoaded(MapAccess.Fingerprint(character.TileConfig));
        ShipIndicator.Instance.ZeeBuilt();
    }
}

/// <summary>The game calls this every frame; its in-map branch is the chart's tick, and ours rides along.</summary>
[HarmonyPatch(typeof(MapProvider), nameof(MapProvider.ListenKeyboardEvents))]
internal static class ChartTickPatch
{
    private static void Postfix() => ChartClicks.Tick();
}

[HarmonyPatch(typeof(MapProvider), nameof(MapProvider.Show))]
internal static class ChartShownPatch
{
    private static void Postfix() => MapDump.OnChartShown();
}
