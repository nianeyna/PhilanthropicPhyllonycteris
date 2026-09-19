using System.Collections.Generic;
using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using Sunless.Game.ApplicationProviders;
using Sunless.Game.Scripts.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>Right-clicks on the open chart: on the marker removes it, anywhere else on the map puts it there.</summary>
internal static class ChartClicks
{
    // A floor for the marker's click radius, so a marker drawn small is still easy to hit.
    private const float MinimumHitRadiusPixels = 12f;

    private static readonly List<RaycastResult> Hits = new List<RaycastResult>();

    /// <summary>Runs every frame from the game's own chart tick.</summary>
    public static void Tick()
    {
        var state = GameProvider.Instance.CurrentUIState;
        if (!state.InMap || state.InMenu || state.InModal) return;
        if (!RightClickedThisFrame() || PointerIsOverAControl()) return;

        var root = MapAccess.MapRoot;
        var camera = MapAccess.MapCamera;
        if (root == null || camera == null) return;

        Vector2 pointer = Input.mousePosition;
        if (ChartMarker.Instance.TryGetScreenCircle(camera, out var centre, out var radius)
            && MarkerHitTest.Hits(pointer.x, pointer.y, centre.x, centre.y, Mathf.Max(radius, MinimumHitRadiusPixels)))
        {
            MarkerStore.Instance.Clear();
            return;
        }

        var local = root.transform.InverseTransformPoint(camera.ScreenToWorldPoint(pointer));
        var chart = new ChartPoint(local.x, local.y);
        if (!ChartSpace.Contains(chart, MapAccess.MapWidth, MapAccess.MapHeight)) return;
        MarkerStore.Instance.Set(ChartSpace.ToWorld(chart));
    }

    // Ctrl+click is a right-click on a Mac everywhere else in the game, and the chart's own drag code skips it.
    private static bool RightClickedThisFrame() =>
        Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && OnClickHandler.MacCtrlPressed);

    // Only things that take clicks count (close, zoom). The chart's labels are uGUI text too, and
    // a click on one of those is still a click on the chart.
    private static bool PointerIsOverAControl()
    {
        var events = EventSystem.current;
        if (events == null) return false;
        Hits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, Hits);
        foreach (var hit in Hits)
        {
            if (hit.gameObject != null && hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
        }
        return false;
    }
}
