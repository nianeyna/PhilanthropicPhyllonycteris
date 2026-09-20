using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The marker's icon on the chart. Rebuilt from the store whenever the marker or the map changes;
/// the game destroys it along with the rest of the chart when the zee is torn down.
/// </summary>
internal sealed class ChartMarker
{
    private const string ObjectName = "PhilanthropicPhyllonycteris Marker";

    // Width of the fallback cross in chart units at the game's icon scale of 1, to match the
    // game's bat and port icons at 5.8; a chart tile is 150 across.
    private const float CrossUnitsAcross = 5.8f;

    private const float InFrontBy = 0.1f;

    // The disc of the game's own boat icon.
    private static readonly Color DiscColour = new Color32(183, 134, 57, 255);

    private GameObject? _marker;
    private SpriteRenderer? _renderer;

    public static ChartMarker Instance { get; } = new ChartMarker();

    private ChartMarker()
    {
    }

    public void Initialise()
    {
        MarkerStore.Instance.Changed += Rebuild;
    }

    /// <summary>The marker's centre and half its width on screen, for telling a click on it from a click beside it.</summary>
    public bool TryGetScreenCircle(Camera camera, out Vector2 centre, out float radius)
    {
        centre = default;
        radius = 0f;
        if (_renderer == null) return false;
        var bounds = _renderer.bounds;
        centre = camera.WorldToScreenPoint(bounds.center);
        radius = Mathf.Abs(camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, 0f)).x - centre.x);
        return true;
    }

    private void Rebuild()
    {
        if (_marker != null)
        {
            MapAccess.Icons?.Remove(_marker);
            Object.Destroy(_marker);
        }
        _marker = null;
        _renderer = null;

        var root = MapAccess.MapRoot;
        var position = MarkerStore.Instance.Current;
        if (root == null || position == null) return;

        _marker = new GameObject(ObjectName);
        _marker.transform.SetParent(root.transform, worldPositionStays: false);
        _marker.layer = (MapAccess.BoatIcon != null ? MapAccess.BoatIcon : root).layer;

        var chart = ChartSpace.ToChart(position.Value);
        var world = root.transform.TransformPoint(new Vector3(chart.X, chart.Y, 0f));
        world.z = DepthInFront();
        _marker.transform.position = world;

        _renderer = _marker.AddComponent<SpriteRenderer>();
        _renderer.sprite = Sprites.Bat() ?? Sprites.Cross(CrossUnitsAcross);
        _renderer.color = DiscColour;
        SortAboveFog(_renderer);

        // The game resizes everything in this list each frame the chart is open.
        MapAccess.Icons?.Add(_marker);
    }

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
        var z = -0.5f;
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
}
