using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PhilanthropicPhyllonycteris.Core;
using Sunless.Game.ApplicationProviders;
using Sunless.Game.Entities.Geography;
using Sunless.Game.Utilities;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.Game;

/// <summary>
/// The one place that reaches into <see cref="MapProvider"/>'s private fields. If the game ever
/// renames one, <see cref="Available"/> is false, one error is logged and the mod stands down
/// rather than throwing every frame.
/// </summary>
internal static class MapAccess
{
    // MapProvider.Prefabs.BatIcon; that class is private and its constants are inlined where used.
    private const string BatIconPrefab = "Map/Icons/Ico_bat";

    private static AccessTools.FieldRef<MapProvider, GameObject>? _mapRoot;
    private static AccessTools.FieldRef<MapProvider, Camera>? _mapCamera;
    private static AccessTools.FieldRef<MapProvider, List<GameObject>>? _icons;
    private static AccessTools.FieldRef<MapProvider, float>? _mapWidth;
    private static AccessTools.FieldRef<MapProvider, float>? _mapHeight;
    private static AccessTools.FieldRef<MapProvider, GameObject>? _boatIcon;
    private static AccessTools.FieldRef<MapProvider, GameObject>? _mapFoU;
    private static AccessTools.FieldRef<MapProvider, Dictionary<GameObject, PortDatum>>? _ports;
    private static AccessTools.FieldRef<MapProvider, GameObject>? _labelsSmall;

    public static bool Available { get; private set; }

    public static void Initialise()
    {
        try
        {
            _mapRoot = AccessTools.FieldRefAccess<MapProvider, GameObject>("_mapRoot");
            _mapCamera = AccessTools.FieldRefAccess<MapProvider, Camera>("_mapCamera");
            _icons = AccessTools.FieldRefAccess<MapProvider, List<GameObject>>("_icons");
            _mapWidth = AccessTools.FieldRefAccess<MapProvider, float>("_mapWidth");
            _mapHeight = AccessTools.FieldRefAccess<MapProvider, float>("_mapHeight");
            _boatIcon = AccessTools.FieldRefAccess<MapProvider, GameObject>("_boatIcon");
            _mapFoU = AccessTools.FieldRefAccess<MapProvider, GameObject>("_mapFoU");
            _ports = AccessTools.FieldRefAccess<MapProvider, Dictionary<GameObject, PortDatum>>("_ports");
            _labelsSmall = AccessTools.FieldRefAccess<MapProvider, GameObject>("_labelsSmallGO");
            Available = true;
        }
        catch (Exception e)
        {
            Available = false;
            Plugin.Log.LogError($"The chart's internals are not where this mod expects them, so it is standing down: {e.Message}");
        }
    }

    private static MapProvider Map => MapProvider.Instance;

    /// <summary>The chart's root; chart coordinates are local to it. Null until the zee has been built.</summary>
    public static GameObject? MapRoot => Available ? _mapRoot!(Map) : null;
    public static Camera? MapCamera => Available ? _mapCamera!(Map) : null;
    /// <summary>The icons the game rescales every frame so they hold their size on screen as the chart zooms.</summary>
    public static List<GameObject>? Icons => Available ? _icons!(Map) : null;
    public static float MapWidth => Available ? _mapWidth!(Map) : 0f;
    public static float MapHeight => Available ? _mapHeight!(Map) : 0f;
    public static GameObject? BoatIcon => Available ? _boatIcon!(Map) : null;
    public static GameObject? FogOfUnknown => Available ? _mapFoU!(Map) : null;
    public static GameObject? AnyPortIcon => Available ? _ports!(Map)?.Keys.FirstOrDefault(p => p != null) : null;
    public static GameObject? SmallLabels => Available ? _labelsSmall!(Map) : null;

    /// <summary>The sprite of the icon the game puts on the chart for what the zee-bat found. Null if the prefab is gone.</summary>
    public static Sprite? BatIconSprite
    {
        get
        {
            var prefab = PrefabHelper.Instance.Get(BatIconPrefab, supressWarning: true);
            var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true) : null;
            return renderer != null ? renderer.sprite : null;
        }
    }

    /// <summary>The fingerprint of a map, the one way for every caller so stored markers and fresh maps agree.</summary>
    public static string Fingerprint(SerializableTileConfig config) =>
        MapFingerprint.Compute(config.Width, config.Height, config.Tiles.Select(t => t.Name));
}
