using PhilanthropicPhyllonycteris.Core;
using Xunit;

namespace PhilanthropicPhyllonycteris.Tests;

public class GeometryTests
{
    private const float Tolerance = 0.001f;

    #region ChartSpace

    // A six by six map in chart units.
    private const float MapSize = 900f;

    [Fact]
    public void Chart_and_world_round_trip_at_a_tenth_scale()
    {
        var world = ChartSpace.ToWorld(new ChartPoint(431.25f, 20.5f));
        Assert.Equal(new ZeePoint(4312.5f, 205f), world);
        Assert.Equal(new ChartPoint(431.25f, 20.5f), ChartSpace.ToChart(world));
    }

    [Theory]
    [InlineData(0f, 0f, true)]
    [InlineData(MapSize, MapSize, true)]
    [InlineData(450f, 450f, true)]
    [InlineData(-0.1f, 450f, false)]
    [InlineData(450f, MapSize + 0.1f, false)]
    public void Only_points_on_the_map_are_contained(float x, float y, bool contained)
    {
        Assert.Equal(contained, ChartSpace.Contains(new ChartPoint(x, y), MapSize, MapSize));
    }

    #endregion

    #region IndicatorMath

    private static readonly ZeePoint Ship = new ZeePoint(1000f, 2000f);

    private static ZeePoint FromShip(float dx, float dy) => new ZeePoint(Ship.X + dx, Ship.Y + dy);

    [Theory]
    [InlineData(500f, 0f, 0f)]
    [InlineData(500f, 500f, 45f)]
    [InlineData(0f, 500f, 90f)]
    [InlineData(-500f, 500f, 135f)]
    [InlineData(-500f, -500f, -135f)]
    [InlineData(500f, -500f, -45f)]
    public void Bearing_is_anticlockwise_from_east(float dx, float dy, float degrees)
    {
        Assert.Equal(degrees, IndicatorMath.BearingDegrees(Ship, FromShip(dx, dy)), Tolerance);
    }

    [Fact]
    public void Arrow_sits_on_the_ring_between_ship_and_marker()
    {
        var marker = FromShip(-300f, 400f);
        var arrow = IndicatorMath.RingPosition(Ship, marker, 50f);

        Assert.Equal(50f, IndicatorMath.Distance(Ship, arrow), Tolerance);
        Assert.Equal(450f, IndicatorMath.Distance(arrow, marker), Tolerance);
    }

    [Fact]
    public void Arrow_hides_once_the_ship_is_near_the_marker()
    {
        Assert.True(IndicatorMath.IsVisible(Ship, FromShip(0f, 91f), hideWithin: 90f));
        Assert.False(IndicatorMath.IsVisible(Ship, FromShip(0f, 89f), hideWithin: 90f));
        Assert.False(IndicatorMath.IsVisible(Ship, Ship, hideWithin: 90f));
    }

    #endregion

    #region ArrivalTracker

    private const float Within = 100f;

    [Fact]
    public void Sailing_up_to_the_marker_is_arriving()
    {
        var tracker = new ArrivalTracker();
        var marker = FromShip(0f, 500f);

        Assert.False(tracker.HasArrived(Ship, marker, Within));
        Assert.True(tracker.HasArrived(FromShip(0f, 450f), marker, Within));
    }

    [Fact]
    public void A_marker_placed_beside_the_ship_stays_until_the_ship_has_left_and_come_back()
    {
        var tracker = new ArrivalTracker();
        var marker = FromShip(0f, 50f);

        Assert.False(tracker.HasArrived(Ship, marker, Within));
        Assert.False(tracker.HasArrived(FromShip(0f, 300f), marker, Within));
        Assert.True(tracker.HasArrived(Ship, marker, Within));
    }

    [Fact]
    public void Having_been_away_from_one_marker_does_not_count_for_the_next()
    {
        var tracker = new ArrivalTracker();
        tracker.HasArrived(Ship, FromShip(0f, 500f), Within);

        Assert.False(tracker.HasArrived(Ship, FromShip(0f, 50f), Within));
    }

    [Fact]
    public void A_reset_forgets_having_been_away()
    {
        var tracker = new ArrivalTracker();
        var marker = FromShip(0f, 500f);
        tracker.HasArrived(Ship, marker, Within);
        tracker.Reset();

        Assert.False(tracker.HasArrived(FromShip(0f, 450f), marker, Within));
    }

    #endregion

    #region MarkerHitTest

    [Theory]
    [InlineData(100f, 100f, true)]
    [InlineData(110f, 110f, true)]
    [InlineData(100f, 116f, true)]
    [InlineData(112f, 112f, false)]
    [InlineData(100f, 83f, false)]
    public void A_click_hits_the_marker_within_its_radius(float clickX, float clickY, bool hit)
    {
        Assert.Equal(hit, MarkerHitTest.Hits(clickX, clickY, 100f, 100f, radiusPixels: 16f));
    }

    #endregion
}
