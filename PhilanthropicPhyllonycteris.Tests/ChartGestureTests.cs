using PhilanthropicPhyllonycteris.Core;
using Xunit;

namespace PhilanthropicPhyllonycteris.Tests;

public class ChartGestureTests
{
    private const int NoHit = MarkerHitTest.None;
    private const float Soon = ChartGesture.DoubleClickSeconds / 2f;
    private const float Late = ChartGesture.DoubleClickSeconds * 2f;
    private const float Nudge = ChartGesture.DragThresholdPixels / 2f;
    private const float Pull = ChartGesture.DragThresholdPixels * 2f;
    private const float Near = ChartGesture.DoubleClickPixels / 2f;
    private const float Far = ChartGesture.DoubleClickPixels * 2f;

    private readonly ChartGesture _gesture = new ChartGesture();

    #region Frames

    private Gesture Down(float time, int hit, float x = 100f, float y = 100f, bool onMap = true) =>
        _gesture.Frame(time, down: true, held: true, up: false, x, y, hit, onMap);

    private Gesture Held(float time, float x = 100f, float y = 100f) =>
        _gesture.Frame(time, down: false, held: true, up: false, x, y, NoHit, onMap: true);

    private Gesture Up(float time, float x = 100f, float y = 100f) =>
        _gesture.Frame(time, down: false, held: false, up: true, x, y, NoHit, onMap: true);

    private Gesture Idle(float time) =>
        _gesture.Frame(time, down: false, held: false, up: false, 100f, 100f, NoHit, onMap: true);

    private static void AssertNone(Gesture gesture) => Assert.Equal(GestureCommand.None, gesture.Command);

    private static void AssertIs(GestureCommand command, int index, Gesture gesture)
    {
        Assert.Equal(command, gesture.Command);
        Assert.Equal(index, gesture.Index);
    }

    // A click on marker 2 at (100, 100), which removes it as the button comes up.
    private void ClickAway(float time = 0f)
    {
        AssertNone(Down(time, hit: 2));
        AssertIs(GestureCommand.Remove, 2, Up(time + 0.02f));
    }

    #endregion

    #region Appending

    [Fact]
    public void A_click_on_open_chart_appends_at_once()
    {
        AssertIs(GestureCommand.Append, NoHit, Down(0f, NoHit));
        AssertNone(Up(0.02f));
    }

    [Fact]
    public void A_click_off_the_map_does_nothing()
    {
        AssertNone(Down(0f, NoHit, onMap: false));
    }

    #endregion

    #region Removing and clearing

    [Fact]
    public void A_click_on_a_marker_removes_it_as_the_button_comes_up()
    {
        ClickAway();
        AssertNone(Idle(1f));
    }

    [Fact]
    public void A_second_click_soon_after_on_the_same_spot_clears_the_path()
    {
        ClickAway();

        // The marker is gone, so the second click lands on open chart.
        AssertIs(GestureCommand.Clear, NoHit, Down(Soon, NoHit, 100f + Near, 100f));
        AssertNone(Up(Soon + 0.02f));
    }

    [Fact]
    public void A_second_click_on_the_same_spot_clears_even_if_another_marker_is_now_under_it()
    {
        ClickAway();

        AssertIs(GestureCommand.Clear, NoHit, Down(Soon, hit: 1));
    }

    [Fact]
    public void A_second_click_soon_after_but_elsewhere_is_an_ordinary_click()
    {
        ClickAway();

        AssertIs(GestureCommand.Append, NoHit, Down(Soon, NoHit, 100f + Far, 100f));
    }

    [Fact]
    public void A_second_click_on_the_same_spot_but_late_is_an_ordinary_click()
    {
        ClickAway();

        AssertIs(GestureCommand.Append, NoHit, Down(Late, NoHit));
    }

    [Fact]
    public void A_third_click_on_the_same_spot_does_not_clear_again()
    {
        ClickAway();
        Down(Soon, NoHit);
        Up(Soon + 0.02f);

        AssertIs(GestureCommand.Append, NoHit, Down(Soon + 0.04f, NoHit));
    }

    [Fact]
    public void Closing_the_chart_forgets_the_click_just_made()
    {
        ClickAway();
        AssertNone(_gesture.Reset());

        AssertIs(GestureCommand.Append, NoHit, Down(Soon, NoHit));
    }

    #endregion

    #region Dragging

    [Fact]
    public void Pressing_on_a_marker_and_moving_drags_it_until_the_button_is_let_go()
    {
        AssertNone(Down(0f, hit: 1));
        AssertNone(Held(0.02f, 100f + Nudge, 100f));
        AssertIs(GestureCommand.Preview, 1, Held(0.04f, 100f + Pull, 100f));
        AssertIs(GestureCommand.Preview, 1, Held(0.06f, 100f + Pull * 2f, 100f));
        AssertIs(GestureCommand.Commit, 1, Up(0.08f, 100f + Pull * 2f, 100f));
        AssertNone(Idle(1f));
    }

    [Fact]
    public void A_press_that_barely_moves_is_a_click()
    {
        Down(0f, hit: 1);
        AssertNone(Held(0.02f, 100f + Nudge, 100f + Nudge));
        AssertIs(GestureCommand.Remove, 1, Up(0.04f, 100f + Nudge, 100f + Nudge));
    }

    [Fact]
    public void Letting_go_without_an_up_event_still_ends_the_drag()
    {
        Down(0f, hit: 0);
        Held(0.02f, 100f + Pull, 100f);

        AssertIs(GestureCommand.Commit, 0, Idle(0.04f));
    }

    [Fact]
    public void Closing_the_chart_mid_drag_commits_where_it_is()
    {
        Down(0f, hit: 0);
        Held(0.02f, 100f + Pull, 100f);

        AssertIs(GestureCommand.Commit, 0, _gesture.Reset());
    }

    #endregion
}
