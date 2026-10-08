using System.Collections.Generic;
using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The path on the chart: an icon per marker, and a line from the ship's icon through them in
/// order. Brought up to date from the store whenever the path or the map changes, in place, so
/// a drag (which changes the path every frame) moves icons rather than remaking them. The game
/// destroys it all along with the rest of the chart when the zee is torn down.
/// </summary>
internal sealed class ChartMarker
{
    private const string ObjectName = "PhilanthropicPhyllonycteris Marker";
    private const string LineObjectName = "PhilanthropicPhyllonycteris Route";

    // Width of the fallback cross in chart units at the game's icon scale of 1, to match the
    // game's bat and port icons at 5.8; a chart tile is 150 across.
    private const float CrossUnitsAcross = 5.8f;

    // The route line, in chart units; it scales with the chart, unlike the icons.
    private const float LineWidth = 0.6f;
    private const float LineAlpha = 0.6f;
    private const string LineShader = "Sprites/Default";

    // A floor for an icon's click radius, so a marker drawn small is still easy to hit.
    private const float MinimumHitRadiusPixels = 12f;

    // Where the game puts its port icons and its own bat mark; the marker is never behind that.
    private const float IconDepth = -0.5f;
    private const float InFrontBy = 0.1f;

    // The disc of the game's own boat icon.
    private static readonly Color DiscColour = new Color32(183, 134, 57, 255);

    private readonly List<GameObject> _icons = new List<GameObject>();
    private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    private readonly List<HitCircle> _circles = new List<HitCircle>();
    private LineRenderer? _line;
    private Material? _lineMaterial;
    private bool _lineShaderMissing;

    public static ChartMarker Instance { get; } = new ChartMarker();

    private ChartMarker()
    {
    }

    public void Initialise()
    {
        MarkerStore.Instance.Changed += Sync;
    }

    /// <summary>The chart has just opened, and the game has just moved its boat icon to the ship, so the line's start is stale.</summary>
    public void ChartShown() => Sync();

    /// <summary>Which marker's icon is under the pointer, or <see cref="MarkerHitTest.None"/>.</summary>
    public int HitTest(Camera camera, Vector2 pointer)
    {
        _circles.Clear();
        foreach (var renderer in _renderers)
        {
            var bounds = renderer.bounds;
            var centre = camera.WorldToScreenPoint(bounds.center);
            var radius = Mathf.Abs(camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, 0f)).x - centre.x);
            _circles.Add(new HitCircle(centre.x, centre.y, Mathf.Max(radius, MinimumHitRadiusPixels)));
        }
        return MarkerHitTest.Nearest(pointer.x, pointer.y, _circles);
    }

    private void Sync()
    {
        var root = MapAccess.MapRoot;
        var path = MarkerStore.Instance.Path;
        DropStale(root);
        if (root == null) return;

        var depth = DepthInFront();
        while (_icons.Count > path.Count) DestroyIcon(_icons.Count - 1);
        while (_icons.Count < path.Count) CreateIcon(root);
        for (var i = 0; i < path.Count; i++)
        {
            var chart = ChartSpace.ToChart(path[i]);
            _icons[i].transform.position = AtDepth(root.transform.TransformPoint(new Vector3(chart.X, chart.Y, 0f)), depth);
        }
        SyncLine(root, path, depth);
    }

    #region Icons

    // Every chart object is new after a load; anything of ours not under the current root is gone or orphaned.
    private void DropStale(GameObject? root)
    {
        for (var i = _icons.Count - 1; i >= 0; i--)
        {
            var icon = _icons[i];
            if (icon != null && root != null && icon.transform.parent == root.transform) continue;
            if (icon != null) Object.Destroy(icon);
            _icons.RemoveAt(i);
            _renderers.RemoveAt(i);
        }
        if (_line != null && (root == null || _line.transform.parent != root.transform))
        {
            Object.Destroy(_line.gameObject);
            _line = null;
        }
    }

    private void CreateIcon(GameObject root)
    {
        var icon = new GameObject(ObjectName);
        icon.transform.SetParent(root.transform, worldPositionStays: false);
        icon.layer = IconLayer(root);

        var renderer = icon.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprites.Bat() ?? Sprites.Cross(CrossUnitsAcross);
        renderer.color = DiscColour;
        SortAboveFog(renderer);

        // The game resizes everything in this list each frame the chart is open.
        MapAccess.Icons?.Add(icon);
        _icons.Add(icon);
        _renderers.Add(renderer);
    }

    private void DestroyIcon(int index)
    {
        var icon = _icons[index];
        MapAccess.Icons?.Remove(icon);
        Object.Destroy(icon);
        _icons.RemoveAt(index);
        _renderers.RemoveAt(index);
    }

    private static int IconLayer(GameObject root) => (MapAccess.BoatIcon != null ? MapAccess.BoatIcon : root).layer;

    private static Vector3 AtDepth(Vector3 world, float depth) => new Vector3(world.x, world.y, depth);

    #endregion

    #region Route line

    // From the ship's icon (which the game moves to the ship each time the chart opens) through
    // the path in order, under the icons. Drawn in chart units, so it scales with the chart.
    private void SyncLine(GameObject root, IReadOnlyList<ZeePoint> path, float depth)
    {
        if (path.Count == 0 || MapAccess.BoatIcon == null)
        {
            if (_line != null) _line.enabled = false;
            return;
        }
        var line = _line != null ? _line : CreateLine(root);
        if (line == null) return;

        var localDepth = root.transform.InverseTransformPoint(new Vector3(0f, 0f, depth)).z;
        var boat = MapAccess.BoatIcon.transform.localPosition;
        line.positionCount = path.Count + 1;
        line.SetPosition(0, new Vector3(boat.x, boat.y, localDepth));
        for (var i = 0; i < path.Count; i++)
        {
            var chart = ChartSpace.ToChart(path[i]);
            line.SetPosition(i + 1, new Vector3(chart.X, chart.Y, localDepth));
        }
        line.enabled = true;
    }

    private LineRenderer? CreateLine(GameObject root)
    {
        var material = LineMaterial();
        if (material == null) return null;

        var lineObject = new GameObject(LineObjectName) { layer = IconLayer(root) };
        lineObject.transform.SetParent(root.transform, worldPositionStays: false);
        _line = lineObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.material = material;
        _line.startWidth = LineWidth;
        _line.endWidth = LineWidth;
        var colour = DiscColour;
        colour.a = LineAlpha;
        _line.startColor = colour;
        _line.endColor = colour;
        _line.numCornerVertices = 4;
        _line.numCapVertices = 2;
        SortAboveFog(_line);
        _line.sortingOrder -= 1;
        return _line;
    }

    private Material? LineMaterial()
    {
        if (_lineMaterial != null) return _lineMaterial;
        if (_lineShaderMissing) return null;
        var shader = Shader.Find(LineShader);
        if (shader == null)
        {
            _lineShaderMissing = true;
            Plugin.Log.LogWarning($"The {LineShader} shader is not available, so the route line is not drawn.");
            return null;
        }
        _lineMaterial = new Material(shader);
        return _lineMaterial;
    }

    #endregion

    #region Depth and sorting

    // Marking unexplored zee is the point, so the marker has to draw over the fog of unknown. The
    // fog is on its own sorting layer ("Particles"), which hides anything on "Default", as it does
    // the port icons. The boat icon's layer ("AboveLights") is the natural home, one above the boat
    // and below the labels (order 10); if that ever stops being above the fog, take the top layer.
    private static void SortAboveFog(Renderer marker)
    {
        var fog = MapAccess.FogOfUnknown != null ? MapAccess.FogOfUnknown.GetComponentInChildren<Renderer>(includeInactive: true) : null;
        var boat = MapAccess.BoatIcon != null ? MapAccess.BoatIcon.GetComponentInChildren<Renderer>(includeInactive: true) : null;
        var fogValue = fog != null ? SortingLayer.GetLayerValueFromID(fog.sortingLayerID) : int.MinValue;
        if (boat != null && SortingLayer.GetLayerValueFromID(boat.sortingLayerID) > fogValue)
        {
            marker.sortingLayerID = boat.sortingLayerID;
            marker.sortingOrder = boat.sortingOrder + 1;
            return;
        }
        var layers = SortingLayer.layers;
        var top = layers[layers.Length - 1];
        marker.sortingLayerID = top.id;
        marker.sortingOrder = top.value == fogValue && fog != null ? fog.sortingOrder + 1 : 0;
    }

    // The map camera looks along +z, so nearer the camera is lower z. Sit just in front of the
    // fog and the boat icon, without crossing the camera's near plane.
    private static float DepthInFront()
    {
        var z = IconDepth;
        foreach (var thing in new[] { MapAccess.FogOfUnknown, MapAccess.BoatIcon })
        {
            if (thing == null) continue;
            foreach (var renderer in thing.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                z = Mathf.Min(z, renderer.transform.position.z);
            }
        }
        z -= InFrontBy;
        var camera = MapAccess.MapCamera;
        if (camera != null) z = Mathf.Max(z, camera.transform.position.z + camera.nearClipPlane + InFrontBy);
        return z;
    }

    #endregion
}
