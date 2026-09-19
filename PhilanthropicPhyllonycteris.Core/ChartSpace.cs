namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// The chart is the zee at one tenth scale: the game places the boat icon at the boat's world
/// position divided by ten, local to the chart's root.
/// </summary>
public static class ChartSpace
{
    public const float WorldUnitsPerChartUnit = 10f;

    public static ZeePoint ToWorld(ChartPoint chart) =>
        new ZeePoint(chart.X * WorldUnitsPerChartUnit, chart.Y * WorldUnitsPerChartUnit);

    public static ChartPoint ToChart(ZeePoint world) =>
        new ChartPoint(world.X / WorldUnitsPerChartUnit, world.Y / WorldUnitsPerChartUnit);

    /// <summary>Whether <paramref name="chart"/> lies on a map of the given size in chart units, edges included.</summary>
    public static bool Contains(ChartPoint chart, float mapWidth, float mapHeight) =>
        chart.X >= 0f && chart.X <= mapWidth && chart.Y >= 0f && chart.Y <= mapHeight;
}
