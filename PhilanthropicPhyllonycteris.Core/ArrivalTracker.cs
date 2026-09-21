namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// Tells arriving at the marker from merely being near it. A marker placed beside the ship, or
/// found beside it when a save is loaded, has not been arrived at until the ship has been away
/// from it and come back.
/// </summary>
public sealed class ArrivalTracker
{
    // The marker the ship was last seen away from. A marker set anywhere else starts afresh.
    private ZeePoint? _awayFrom;

    /// <summary>The ship has been put somewhere new (a save was loaded), so where it was before counts for nothing.</summary>
    public void Reset() => _awayFrom = null;

    /// <summary>Call every frame there is a ship and a marker. True while the ship is within <paramref name="within"/> of a marker it has been farther from.</summary>
    public bool HasArrived(ZeePoint ship, ZeePoint marker, float within)
    {
        if (IndicatorMath.Distance(ship, marker) > within)
        {
            _awayFrom = marker;
            return false;
        }

        return _awayFrom == marker;
    }
}
