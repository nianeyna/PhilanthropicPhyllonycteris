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
        MarkerColour = Config.Bind(
            "Marker",
            "Colour",
            // The disc of the game's own boat icon.
            (Color)new Color32(183, 134, 57, 255),
            "Colour of the marker's disc on the chart, as RRGGBBAA. The default is the yellow of the game's ship icon. Applies live.");
        IndicatorEnabled = Config.Bind(
            "Indicator",
            "Enabled",
            true,
            "Show the arrow by the ship. Turn it off to keep only the marker on the chart. Applies live.");
        RingRadius = Config.Bind(
            "Indicator",
            "RingRadius",
            45f,
            "How far from the ship the arrow floats, in zee units (the view is 300 to 400 of them tall). Applies live.");
        HideWithin = Config.Bind(
            "Indicator",
            "HideWithin",
            90f,
            "The arrow hides once the marker is closer than this, in zee units. Keep it above RingRadius, or the arrow ends up pointing back across the ship. Applies live.");
        IndicatorColour = Config.Bind(
            "Indicator",
            "Colour",
            new Color(1f, 0.82f, 0.3f),
            "Tint of the arrow, as RRGGBBAA. Applies live.");
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
