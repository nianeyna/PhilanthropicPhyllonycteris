using PhilanthropicPhyllonycteris.Core;
using PhilanthropicPhyllonycteris.Game;
using Sunless.Game.ApplicationProviders;
using Sunless.Game.Scripts.Physics;
using UnityEngine;

namespace PhilanthropicPhyllonycteris.UI;

/// <summary>
/// The bat by the ship: the game's own seeking bat with a light spreading from its outline,
/// hovering on an invisible ring around the boat, on the bearing to the path's first marker and
/// heading along it. Being in the world rather than on the HUD, it stays right however the camera is set, and
/// the game's own panels cover it. If the game's bat cannot be had, an arrow stands in.
/// </summary>
internal sealed class ShipIndicator
{
    private const string ObjectName = "PhilanthropicPhyllonycteris Indicator";
    private const string GlowObjectName = "Glow";

    // Widths in world units; the sailing camera shows 300 to 400 of them top to bottom, and the
    // game's seeking bat is 9 across.
    private const float BatUnitsAcross = 10f;
    // How far the light spreads beyond the bat's outline, and how wide the game's disc is if the
    // outline cannot be followed.
    private const float GlowUnitsBeyond = 2f;
    private const float GlowDiscUnitsAcross = 12f;
    private const float ArrowUnitsAcross = 12f;

    // The game's bat is drawn head up, the arrow pointing right.
    private const float BatHeading = 90f;

    // The hover: a slow drift in and out along the bearing. It carries on while the game is paused.
    private const float HoverUnits = 1.5f;
    private const float HoverSeconds = 3f;

    // The layer the game puts its own range indicators on so the darkness overlay does not swallow them.
    private const string AboveLightsLayerName = "AboveLights";
    // The game keeps what the ship sails under on that layer too (bridges, lighthouse caps, statues:
    // most at order 0, the tallest at 4), and equal orders are settled by distance, which changes as
    // the ship goes by. So the bat and its light take the top of the range; orders are 16 bit.
    private const int BatSortingOrder = short.MaxValue;
    private const int GlowSortingOrder = BatSortingOrder - 1;

    private const string PlayerBoatName = "PlayerBoat";

    private const float FadeSeconds = 0.3f;
    private const float BoatSearchInterval = 1f;

    private GameObject? _indicator;
    private SpriteRenderer? _renderer;
    private SpriteRenderer? _glow;
    private float _heading;
    private float _alpha;

    private bool _zeeBuilt;
    private MoveBoat? _boat;
    private float _nextBoatSearch;
    private readonly ArrivalTracker _arrival = new ArrivalTracker();

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
        _arrival.Reset();
    }

    /// <summary>Runs from the plugin's LateUpdate, after the boat has moved for the frame.</summary>
    public void Tick()
    {
        if (!_zeeBuilt) return;
        var boat = FindBoat();
        var marker = MarkerStore.Instance.Next;
        if (boat == null || marker == null)
        {
            Hide();
            return;
        }

        Vector3 boatPosition = boat.transform.position;
        var ship = new ZeePoint(boatPosition.x, boatPosition.y);
        // Arriving is the path's business, so it counts with the bat turned off too.
        var arrived = _arrival.HasArrived(ship, marker.Value, Plugin.HideWithin.Value);
        if (!Plugin.IndicatorEnabled.Value)
        {
            if (arrived) MarkerStore.Instance.Advance();
            Hide();
            return;
        }

        var state = GameProvider.Instance.CurrentUIState;
        var wanted = !state.InDock && !state.InMap
            && IndicatorMath.IsVisible(ship, marker.Value, Plugin.HideWithin.Value);

        // Unscaled, so the bat still fades away if docking stops the clock.
        _alpha = Mathf.MoveTowards(_alpha, wanted ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
        if (_alpha <= 0f)
        {
            // Only now, so the bat fades away on arrival like any other time rather than vanishing
            // with the marker; with more of the path to go it then fades back in toward the next.
            if (arrived) MarkerStore.Instance.Advance();
            Hide();
            return;
        }

        var renderer = Indicator(boat);
        var hover = _glow != null ? HoverUnits * Mathf.Sin(Time.time * 2f * Mathf.PI / HoverSeconds) : 0f;
        var ring = IndicatorMath.RingPosition(ship, marker.Value, Plugin.RingRadius.Value + hover);
        renderer.transform.position = new Vector3(ring.X, ring.Y, boatPosition.z);
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, IndicatorMath.BearingDegrees(ship, marker.Value) - _heading);

        // The config colour is the light's; the bat stays black. The stand-in arrow takes the colour itself.
        var colour = Plugin.IndicatorColour.Value;
        colour.a *= _alpha;
        if (_glow != null)
        {
            _glow.color = colour;
            _glow.enabled = true;
            colour = new Color(1f, 1f, 1f, _alpha);
        }
        renderer.color = colour;
        renderer.enabled = true;
    }

    private void Hide()
    {
        _alpha = 0f;
        if (_renderer != null) _renderer.enabled = false;
        if (_glow != null) _glow.enabled = false;
    }

    // The indicator lives in the zee's scene and dies with it; Unity's null check notices and it is made again.
    private SpriteRenderer Indicator(MoveBoat boat)
    {
        if (_indicator != null && _renderer != null) return _renderer;
        _indicator = new GameObject(ObjectName) { layer = boat.gameObject.layer };
        _renderer = _indicator.AddComponent<SpriteRenderer>();
        _renderer.sortingLayerName = AboveLightsLayerName;
        _renderer.sortingOrder = BatSortingOrder;
        _glow = null;

        if (!SeekingBat.TryGetSprites(out var bat, out var glow))
        {
            Plugin.Log.LogWarning("The game's seeking bat is not available, so the indicator is an arrow instead.");
            _renderer.sprite = Sprites.Chevron(ArrowUnitsAcross);
            _heading = 0f;
            return _renderer;
        }

        _renderer.sprite = bat;
        _indicator.transform.localScale = ScaleTo(bat, BatUnitsAcross);
        _heading = BatHeading;

        var glowObject = new GameObject(GlowObjectName) { layer = _indicator.layer };
        glowObject.transform.SetParent(_indicator.transform, worldPositionStays: false);
        // The light drawn around the bat is already to the bat's scale; the game's disc is not.
        var outline = Sprites.Glow(bat, GlowUnitsBeyond / BatUnitsAcross);
        if (outline == null) glowObject.transform.localScale = ScaleTo(glow, GlowDiscUnitsAcross / _indicator.transform.localScale.x);
        _glow = glowObject.AddComponent<SpriteRenderer>();
        _glow.sprite = outline != null ? outline : glow;
        _glow.sortingLayerName = AboveLightsLayerName;
        _glow.sortingOrder = GlowSortingOrder;
        return _renderer;
    }

    private static Vector3 ScaleTo(Sprite sprite, float unitsAcross)
    {
        var scale = unitsAcross / sprite.bounds.size.x;
        return new Vector3(scale, scale, 1f);
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
