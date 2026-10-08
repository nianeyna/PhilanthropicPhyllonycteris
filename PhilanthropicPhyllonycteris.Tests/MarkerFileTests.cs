using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhilanthropicPhyllonycteris.Core;
using Xunit;

namespace PhilanthropicPhyllonycteris.Tests;

public class MarkerFileTests
{
    private const string ThisMap = "9c1f06e2a4b7d350";
    private const string OtherMap = "0123456789abcdef";

    private static readonly DateTime Placed = new DateTime(2026, 9, 19, 14, 5, 0, DateTimeKind.Utc);

    private static MarkerEntry Marker(float x, float y) => new MarkerEntry(new ZeePoint(x, y), Placed);

    private static MarkerFile TwoMaps()
    {
        var file = new MarkerFile();
        file.Append(ThisMap, Marker(4312.5f, -208.25f));
        file.Append(OtherMap, Marker(0.1f, 8999.999f));
        return file;
    }

    // This map has a path of three, placed in x order; the other map has one marker.
    private static MarkerFile PathOfThree()
    {
        var file = TwoMaps();
        file.Append(ThisMap, Marker(2f, 0f));
        file.Append(ThisMap, Marker(3f, 0f));
        file.Replace(ThisMap, 0, Marker(1f, 0f));
        return file;
    }

    private static MarkerFile Reread(MarkerFile file)
    {
        var problems = new List<string>();
        var reread = MarkerFile.Parse(file.Write(), problems);
        Assert.Empty(problems);
        return reread;
    }

    private static float[] Xs(MarkerFile file, string map) => file.Get(map).Select(m => m.Position.X).ToArray();

    private static string WithLines(params string[] lines) =>
        MarkerFile.Header + "\t" + MarkerFile.Version + "\n" + string.Join("\n", lines) + "\n";

    #region Round trip

    [Fact]
    public void Markers_survive_a_round_trip_exactly()
    {
        var reread = Reread(TwoMaps());

        Assert.Equal(2, reread.MapCount);
        Assert.Equal(new ZeePoint(4312.5f, -208.25f), reread.Get(ThisMap)[0].Position);
        Assert.Equal(new ZeePoint(0.1f, 8999.999f), reread.Get(OtherMap)[0].Position);
        Assert.Equal(Placed, reread.Get(ThisMap)[0].SetAtUtc);
        Assert.Equal(DateTimeKind.Utc, reread.Get(ThisMap)[0].SetAtUtc.Kind);
    }

    [Fact]
    public void A_path_keeps_its_order_through_a_round_trip()
    {
        var reread = Reread(PathOfThree());

        Assert.Equal(new[] { 1f, 2f, 3f }, Xs(reread, ThisMap));
        Assert.Single(reread.Get(OtherMap));
    }

    [Fact]
    public void Output_does_not_depend_on_the_machine_culture()
    {
        var expected = TwoMaps().Write();
        var before = CultureInfo.CurrentCulture;
        try
        {
            // German writes 4312,5.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(expected, TwoMaps().Write());
            Assert.Equal(new ZeePoint(4312.5f, -208.25f), MarkerFile.Parse(expected).Get(ThisMap)[0].Position);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void Empty_text_is_an_empty_file()
    {
        var problems = new List<string>();
        Assert.Equal(0, MarkerFile.Parse("", problems).MapCount);
        Assert.Empty(problems);
    }

    [Fact]
    public void A_map_without_markers_has_an_empty_path()
    {
        Assert.Empty(new MarkerFile().Get(ThisMap));
    }

    #endregion

    #region Changes leave the rest alone

    [Fact]
    public void Replacing_a_marker_keeps_its_place_and_other_maps_lines()
    {
        var file = PathOfThree();
        file.Replace(ThisMap, 1, Marker(10f, 20f));
        var reread = Reread(file);

        Assert.Equal(new[] { 1f, 10f, 3f }, Xs(reread, ThisMap));
        Assert.Equal(new ZeePoint(0.1f, 8999.999f), reread.Get(OtherMap)[0].Position);
    }

    [Fact]
    public void Replacing_an_index_the_path_does_not_have_changes_nothing()
    {
        var file = PathOfThree();
        file.Replace(ThisMap, 3, Marker(10f, 20f));
        file.Replace("unknown", 0, Marker(10f, 20f));

        Assert.Equal(new[] { 1f, 2f, 3f }, Xs(file, ThisMap));
        Assert.Equal(2, file.MapCount);
    }

    [Fact]
    public void Removing_from_the_middle_closes_the_path_up()
    {
        var file = PathOfThree();

        Assert.True(file.RemoveAt(ThisMap, 1));
        Assert.Equal(new[] { 1f, 3f }, Xs(Reread(file), ThisMap));
    }

    [Fact]
    public void Removing_the_last_marker_forgets_the_map()
    {
        var file = TwoMaps();

        Assert.True(file.RemoveAt(ThisMap, 0));
        Assert.False(file.RemoveAt(ThisMap, 0));
        Assert.Equal(1, file.MapCount);
        Assert.Empty(Reread(file).Get(ThisMap));
    }

    [Fact]
    public void Removing_a_map_removes_its_whole_path_and_only_that_map()
    {
        var file = PathOfThree();

        Assert.True(file.Remove(ThisMap));
        Assert.False(file.Remove(ThisMap));

        var reread = Reread(file);
        Assert.Empty(reread.Get(ThisMap));
        Assert.Single(reread.Get(OtherMap));
    }

    #endregion

    #region Damage

    [Theory]
    [InlineData("not a marker at all")]
    [InlineData(ThisMap + "\t12.5\t7")]
    [InlineData(ThisMap + "\ttwelve\t7\t2026-09-19T14:05:00Z")]
    [InlineData(ThisMap + "\tNaN\t7\t2026-09-19T14:05:00Z")]
    [InlineData(ThisMap + "\t12,5\t7\tyesterday")]
    public void A_damaged_line_is_reported_and_the_rest_kept(string damaged)
    {
        var problems = new List<string>();
        var file = MarkerFile.Parse(WithLines(damaged, OtherMap + "\t1\t2\t2026-09-19T14:05:00Z"), problems);

        Assert.Single(problems);
        Assert.Empty(file.Get(ThisMap));
        Assert.Equal(new ZeePoint(1f, 2f), file.Get(OtherMap)[0].Position);
    }

    [Fact]
    public void Another_format_version_is_refused_whole()
    {
        var problems = new List<string>();
        var text = MarkerFile.Header + "\t" + (MarkerFile.Version + 1) + "\n" + ThisMap + "\t1\t2\t2026-09-19T14:05:00Z\n";

        Assert.Equal(0, MarkerFile.Parse(text, problems).MapCount);
        Assert.Single(problems);
    }

    [Fact]
    public void A_fingerprint_that_would_break_the_format_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new MarkerFile().Append("two\tfields", Marker(1f, 2f)));
    }

    #endregion
}
