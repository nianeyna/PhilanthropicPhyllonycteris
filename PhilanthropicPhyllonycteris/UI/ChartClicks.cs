using System.Collections.Generic;
using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using Sunless.Game.ApplicationProviders;
using Sunless.Game.Scripts.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The right mouse button on the open chart, read each frame and handed to
/// <see cref="ChartGesture"/>, whose answer is carried out against the store: a click on open
/// chart appends a marker, a click on a marker removes it, a second click on the same spot soon
/// after clears the path, and a press-and-drag moves one. Never touches a GameObject; the chart
/// follows the store.
/// </summary>
internal static class ChartClicks
{
    private static readonly List<RaycastResult> Hits = new List<RaycastResult>();
    private static readonly ChartGesture Gesture = new ChartGesture();

    /// <summary>Runs every frame from the game's own chart tick.</summary>
    public static void Tick()
    {
        var state = GameProvider.Instance.CurrentUIState;
        if (!state.InMap || state.InMenu || state.InModal)
        {
            Apply(Gesture.Reset(), default);
            return;
        }
        var root = MapAccess.MapRoot;
        var camera = MapAccess.MapCamera;
        if (root == null || camera == null) return;

        var down = ButtonDown();
        // A press over a control (close, zoom) is theirs, but a drag may pass over one on its way.
        if (down && PointerIsOverAControl()) return;

        Vector2 pointer = Input.mousePosition;
        var held = ButtonHeld();
        var up = ButtonUp();
        var chart = ToChart(root, camera, pointer);
        var onMap = ChartSpace.Contains(chart, MapAccess.MapWidth, MapAccess.MapHeight);
        var hit = down ? ChartMarker.Instance.HitTest(camera, pointer) : MarkerHitTest.None;
        Apply(Gesture.Frame(Time.unscaledTime, down, held, up, pointer.x, pointer.y, hit, onMap), chart);
    }

    private static void Apply(Gesture gesture, ChartPoint chart)
    {
        var store = MarkerStore.Instance;
        switch (gesture.Command)
        {
            case GestureCommand.Append:
                store.Append(ChartSpace.ToWorld(chart));
                break;
            case GestureCommand.Remove:
                store.RemoveAt(gesture.Index);
                break;
            case GestureCommand.Clear:
                store.Clear();
                break;
            case GestureCommand.Preview:
                store.Preview(gesture.Index, ChartSpace.ToWorld(ChartSpace.Clamp(chart, MapAccess.MapWidth, MapAccess.MapHeight)));
                break;
            case GestureCommand.Commit:
                store.Commit();
                break;
        }
    }

    private static ChartPoint ToChart(GameObject root, Camera camera, Vector2 pointer)
    {
        var local = root.transform.InverseTransformPoint(camera.ScreenToWorldPoint(pointer));
        return new ChartPoint(local.x, local.y);
    }

    #region The right button

    // Ctrl+click is a right-click on a Mac everywhere else in the game, and the chart's own drag
    // code (left button only) skips it, so a ctrl+left drag is free too.
    private static bool ButtonDown() =>
        Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && OnClickHandler.MacCtrlPressed);

    private static bool ButtonHeld() =>
        Input.GetMouseButton(1) || (Input.GetMouseButton(0) && OnClickHandler.MacCtrlPressed);

    private static bool ButtonUp() =>
        Input.GetMouseButtonUp(1) || (Input.GetMouseButtonUp(0) && OnClickHandler.MacCtrlPressed);

    #endregion

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
