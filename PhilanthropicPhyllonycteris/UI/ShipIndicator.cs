using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using Sunless.Game.ApplicationProviders;
using Sunless.Game.Scripts.Physics;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The arrow by the ship: a world-space sprite on an invisible ring around the boat, on the
/// bearing to the marker and pointing along it. Being in the world rather than on the HUD, it
/// stays right however the camera is set, and the game's own panels cover it.
/// </summary>
internal sealed class ShipIndicator
{
    private const string ObjectName = "PhilanthropicPhyllonycteris Arrow";

    // Width in world units; the sailing camera shows 300 to 400 of them top to bottom.
    private const float UnitsAcross = 12f;

    // The layer the game puts its own range indicators on so the darkness overlay does not swallow them.
    private const string AboveLightsLayerName = "AboveLights";

    private const string PlayerBoatName = "PlayerBoat";

    private const float FadeSeconds = 0.3f;
    private const float BoatSearchInterval = 1f;

    private GameObject? _arrow;
    private SpriteRenderer? _renderer;
    private float _alpha;

    private bool _zeeBuilt;
    private MoveBoat? _boat;
    private float _nextBoatSearch;

    public static ShipIndicator Instance { get; } = new ShipIndicator();

    private ShipIndicator()
    {
    }

    /// <summary>The zee has been built, so there is, or soon will be, a boat to find.</summary>
    public void ZeeBuilt()
    {
        _zeeBuilt = true;
        _boat = null;
        _nextBoatSearch = 0f;
    }

    /// <summary>Runs from the plugin's LateUpdate, after the boat has moved for the frame.</summary>
    public void Tick()
    {
        if (!_zeeBuilt) return;
        var boat = FindBoat();
        var marker = MarkerStore.Instance.Current;
        if (boat == null || marker == null || !Plugin.IndicatorEnabled.Value)
        {
            Hide();
            return;
        }

        Vector3 boatPosition = boat.transform.position;
        var ship = new ZeePoint(boatPosition.x, boatPosition.y);
        var state = GameProvider.Instance.CurrentUIState;
        var wanted = !state.InDock && !state.InMap
            && IndicatorMath.IsVisible(ship, marker.Value, Plugin.HideWithin.Value);

        // Unscaled, so the arrow still fades away if docking stops the clock.
        _alpha = Mathf.MoveTowards(_alpha, wanted ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
        if (_alpha <= 0f)
        {
            Hide();
            return;
        }

        var renderer = Arrow(boat);
        var ring = IndicatorMath.RingPosition(ship, marker.Value, Plugin.RingRadius.Value);
        renderer.transform.position = new Vector3(ring.X, ring.Y, boatPosition.z);
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, IndicatorMath.BearingDegrees(ship, marker.Value));
        var colour = Plugin.IndicatorColour.Value;
        colour.a *= _alpha;
        renderer.color = colour;
        renderer.enabled = true;
    }

    private void Hide()
    {
        _alpha = 0f;
        if (_renderer != null) _renderer.enabled = false;
    }

    // The arrow lives in the zee's scene and dies with it; Unity's null check notices and it is made again.
    private SpriteRenderer Arrow(MoveBoat boat)
    {
        if (_arrow != null && _renderer != null) return _renderer;
        _arrow = new GameObject(ObjectName) { layer = boat.gameObject.layer };
        _renderer = _arrow.AddComponent<SpriteRenderer>();
        _renderer.sprite = Sprites.Chevron(UnitsAcross);
        _renderer.sortingLayerName = AboveLightsLayerName;
        return _renderer;
    }

    // Not NavigationProvider.Boat: that searches the scene on every call while there is no boat. This asks once a second.
    private MoveBoat? FindBoat()
    {
        if (_boat != null) return _boat;
        if (Time.unscaledTime < _nextBoatSearch) return null;
        _nextBoatSearch = Time.unscaledTime + BoatSearchInterval;
        // The name the game itself looks the player's boat up by.
        var found = GameObject.Find(PlayerBoatName);
        _boat = found != null ? found.GetComponent<MoveBoat>() : null;
        if (_boat == null) return null;
        MapDump.OnShipFound(_boat);
        return _boat;
    }
}
