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
    public const string PluginVersion = "0.2.0";

    internal static ManualLogSource Log = null!;

    internal static ConfigEntry<bool> IndicatorEnabled = null!;
    internal static ConfigEntry<float> RingRadius = null!;
    internal static ConfigEntry<float> HideWithin = null!;
    internal static ConfigEntry<Color> IndicatorColour = null!;
    internal static ConfigEntry<bool> DumpMapIcons = null!;
    internal static ConfigEntry<bool> DumpShip = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;
        IndicatorEnabled = Config.Bind(
            "Indicator",
            "Enabled",
            true,
            "Show the bat by the ship. Turn it off to keep only the marker on the chart. Applies live.");
        RingRadius = Config.Bind(
            "Indicator",
            "RingRadius",
            90f,
            "How far from the ship the bat hovers, in zee units (the view is 300 to 400 of them tall). Applies live.");
        HideWithin = Config.Bind(
            "Indicator",
            "HideWithin",
            100f,
            "The bat hides once the marker is closer than this, in zee units, and the marker counts as reached. Keep it above RingRadius, or the bat ends up pointing back across the ship. Applies live.");
        IndicatorColour = Config.Bind(
            "Indicator",
            "Colour",
            new Color(1f, 1f, 1f, 0.9f),
            "Colour of the light around the bat, as RRGGBBAA; the last pair is how strong it is. Applies live.");
        DumpMapIcons = Config.Bind(
            "Debug",
            "DumpMapIcons",
            false,
            "Log how the chart's own icons, fog and labels are built (components, layers, depth, sorting) the first time the chart opens. Development aid.");
        DumpShip = Config.Bind(
            "Debug",
            "DumpShip",
            false,
            "Log how the ship and the sailing camera are built (components, layers, depth, sorting) the first time the ship is found. Development aid.");

        MapAccess.Initialise();
        if (!MapAccess.Available) return;
        MarkerStore.Initialise();
        ChartMarker.Instance.Initialise();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded");
    }

    private void LateUpdate()
    {
        if (MapAccess.Available) ShipIndicator.Instance.Tick();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
