using System;
using System.Collections.Generic;
using System.Globalization;
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
        file.Set(ThisMap, Marker(4312.5f, -208.25f));
        file.Set(OtherMap, Marker(0.1f, 8999.999f));
        return file;
    }

    private static MarkerFile Reread(MarkerFile file)
    {
        var problems = new List<string>();
        var reread = MarkerFile.Parse(file.Write(), problems);
        Assert.Empty(problems);
        return reread;
    }

    private static string WithLines(params string[] lines) =>
        MarkerFile.Header + "\t" + MarkerFile.Version + "\n" + string.Join("\n", lines) + "\n";

    #region Round trip

    [Fact]
    public void Markers_survive_a_round_trip_exactly()
    {
        var reread = Reread(TwoMaps());

        Assert.Equal(2, reread.Count);
        Assert.Equal(new ZeePoint(4312.5f, -208.25f), reread.Get(ThisMap)!.Value.Position);
        Assert.Equal(new ZeePoint(0.1f, 8999.999f), reread.Get(OtherMap)!.Value.Position);
        Assert.Equal(Placed, reread.Get(ThisMap)!.Value.SetAtUtc);
        Assert.Equal(DateTimeKind.Utc, reread.Get(ThisMap)!.Value.SetAtUtc.Kind);
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
            Assert.Equal(new ZeePoint(4312.5f, -208.25f), MarkerFile.Parse(expected).Get(ThisMap)!.Value.Position);
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
        Assert.Equal(0, MarkerFile.Parse("", problems).Count);
        Assert.Empty(problems);
    }

    #endregion

    #region Changes leave other maps alone

    [Fact]
    public void Moving_a_marker_keeps_other_maps_lines()
    {
        var file = TwoMaps();
        file.Set(ThisMap, Marker(10f, 20f));
        var reread = Reread(file);

        Assert.Equal(new ZeePoint(10f, 20f), reread.Get(ThisMap)!.Value.Position);
        Assert.Equal(new ZeePoint(0.1f, 8999.999f), reread.Get(OtherMap)!.Value.Position);
    }

    [Fact]
    public void Removing_a_marker_removes_only_that_map()
    {
        var file = TwoMaps();

        Assert.True(file.Remove(ThisMap));
        Assert.False(file.Remove(ThisMap));

        var reread = Reread(file);
        Assert.Null(reread.Get(ThisMap));
        Assert.NotNull(reread.Get(OtherMap));
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
        Assert.Null(file.Get(ThisMap));
        Assert.Equal(new ZeePoint(1f, 2f), file.Get(OtherMap)!.Value.Position);
    }

    [Fact]
    public void Another_format_version_is_refused_whole()
    {
        var problems = new List<string>();
        var text = MarkerFile.Header + "\t" + (MarkerFile.Version + 1) + "\n" + ThisMap + "\t1\t2\t2026-09-19T14:05:00Z\n";

        Assert.Equal(0, MarkerFile.Parse(text, problems).Count);
        Assert.Single(problems);
    }

    [Fact]
    public void A_fingerprint_that_would_break_the_format_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new MarkerFile().Set("two\tfields", Marker(1f, 2f)));
    }

    #endregion
}
