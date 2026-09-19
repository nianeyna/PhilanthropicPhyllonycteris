using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PhilanthropicPhyllonycteris.Game;
using PhilanthropicPhyllonycteris.UI;
using UnityEngine;

namespace PhilanthropicPhyllonycteris;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "nianeyna.PhilanthropicPhyllonycteris";
    public const string PluginName = "PhilanthropicPhyllonycteris";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log = null!;

    internal static ConfigEntry<Color> MarkerColour = null!;

    internal static ConfigEntry<bool> DumpMapIcons = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;
        MarkerColour = Config.Bind(
            "Marker",
            "Colour",
            new Color(1f, 0.82f, 0.3f),
            "Tint of the marker on the chart, as RRGGBBAA. Applies live.");
        DumpMapIcons = Config.Bind(
            "Debug",
            "DumpMapIcons",
            false,
            "Log how the chart's own icons, fog and labels are built (components, layers, depth, sorting) the first time the chart opens. Development aid.");

        MapAccess.Initialise();
        if (!MapAccess.Available) return;
        MarkerStore.Initialise();
        ChartMarker.Instance.Initialise();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
