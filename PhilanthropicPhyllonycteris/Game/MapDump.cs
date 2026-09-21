using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PhilanthropicPhyllonycteris.Game;

/// <summary>
/// Development aids: log how the game's own pieces are built (components, layers, depth, sorting,
/// shaders), which is what the marker and the indicator have to fit in with. Once each per session.
/// </summary>
internal static class MapDump
{
    private static bool _chartDone;
    private static bool _shipDone;

    /// <summary>Behind <c>Debug.DumpMapIcons</c>: the chart's camera, icons, fog and labels.</summary>
    public static void OnChartShown()
    {
        if (_chartDone || !Plugin.DumpMapIcons.Value) return;
        _chartDone = true;

        var text = new StringBuilder("Chart dump\n");
        var camera = MapAccess.MapCamera;
        if (camera != null)
        {
            text.AppendLine($"Camera: position {camera.transform.position:F2}, forward {camera.transform.forward:F2}, " +
                $"near {camera.nearClipPlane}, far {camera.farClipPlane}, ortho size {camera.orthographicSize}, " +
                $"culling mask 0x{camera.cullingMask:X8}, transparency sort {camera.transparencySortMode}");
        }
        text.AppendLine(SortingLayers());
        Describe(text, "Map root", MapAccess.MapRoot, children: false);
        Describe(text, "Boat icon", MapAccess.BoatIcon, children: true);
        Describe(text, "Port icon", MapAccess.AnyPortIcon, children: true);
        Describe(text, "Fog of unknown", MapAccess.FogOfUnknown, children: true);

        var labels = MapAccess.SmallLabels;
        Describe(text, "Small labels", labels, children: false);
        if (labels != null)
        {
            var canvas = labels.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                text.AppendLine($"  canvas \"{canvas.name}\": {canvas.renderMode}, sorting layer \"{canvas.sortingLayerName}\" order {canvas.sortingOrder}, " +
                    $"raycaster {(canvas.GetComponent<GraphicRaycaster>() != null ? "yes" : "no")}");
            }
            var label = labels.GetComponentInChildren<Text>(includeInactive: true);
            if (label != null) text.AppendLine($"  first label \"{label.name}\": raycastTarget {label.raycastTarget}");
        }
        Plugin.Log.LogInfo(text.ToString());
    }

    /// <summary>Behind <c>Debug.DumpShip</c>: the boat and the sailing camera.</summary>
    public static void OnShipFound(Component boat)
    {
        if (_shipDone || !Plugin.DumpShip.Value) return;
        _shipDone = true;

        var text = new StringBuilder("Ship dump\n");
        text.AppendLine(SortingLayers());
        var camera = Camera.main;
        if (camera != null)
        {
            text.AppendLine($"Camera \"{camera.name}\": position {camera.transform.position:F2}, rotation {camera.transform.eulerAngles:F2}, " +
                $"near {camera.nearClipPlane}, far {camera.farClipPlane}, ortho size {camera.orthographicSize}, culling mask 0x{camera.cullingMask:X8}");
        }
        Describe(text, "Boat", boat.gameObject, children: true);
        Plugin.Log.LogInfo(text.ToString());
    }

    private static string SortingLayers() =>
        "Sorting layers, bottom to top: " + string.Join(", ", SortingLayer.layers.Select(l => $"{l.name} ({l.value})"));

    private static void Describe(StringBuilder text, string title, GameObject? thing, bool children)
    {
        if (thing == null)
        {
            text.AppendLine($"{title}: none");
            return;
        }
        text.AppendLine($"{title}:");
        var transforms = children ? thing.GetComponentsInChildren<Transform>(includeInactive: true) : new[] { thing.transform };
        foreach (var t in transforms)
        {
            var components = string.Join(", ", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
            text.AppendLine($"  \"{t.name}\": layer {t.gameObject.layer} ({LayerMask.LayerToName(t.gameObject.layer)}), " +
                $"world {t.position:F2}, scale {t.lossyScale:F2}, [{components}]");
            var renderer = t.GetComponent<Renderer>();
            if (renderer == null) continue;
            var material = renderer.sharedMaterial;
            text.AppendLine($"    renderer: sorting layer \"{renderer.sortingLayerName}\" order {renderer.sortingOrder}, bounds size {renderer.bounds.size:F2}, " +
                $"shader \"{(material != null ? material.shader.name : "none")}\", queue {(material != null ? material.renderQueue : 0)}");
        }
    }
}
