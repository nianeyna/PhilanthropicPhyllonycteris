namespace PhilanthropicPhyllonycteris.Core;

/// <summary>What a frame of chart input asks for.</summary>
public enum GestureCommand
{
    None,
    /// <summary>Put a new marker at the end of the path, where the pointer is.</summary>
    Append,
    /// <summary>Take the marker at <see cref="Gesture.Index"/> out of the path.</summary>
    Remove,
    /// <summary>Forget the whole path.</summary>
    Clear,
    /// <summary>Show the marker at <see cref="Gesture.Index"/> where the pointer is, without saving.</summary>
    Preview,
    /// <summary>The drag has ended; save the path as it is.</summary>
    Commit,
}

public readonly struct Gesture
{
    public static readonly Gesture None = new Gesture(GestureCommand.None, MarkerHitTest.None);

    public GestureCommand Command { get; }
    public int Index { get; }

    public Gesture(GestureCommand command, int index)
    {
        Command = command;
        Index = index;
    }
}

/// <summary>
/// Turns the right mouse button's doings on the open chart into path changes, one per frame at
/// most: a click on open chart appends, a click on a marker removes it at once, a second click
/// soon after on the spot the marker was clears the path, and pressing on a marker and moving
/// drags it. Pure, so the timing can be tested; the caller supplies the clock and the hit test.
/// </summary>
public sealed class ChartGesture
{
    /// <summary>How long after a marker is clicked away a click on the same spot still counts as the double.</summary>
    public const float DoubleClickSeconds = 0.35f;
    /// <summary>How near the second click must land to where the first was pressed, in pixels.</summary>
    public const float DoubleClickPixels = 12f;
    /// <summary>How far the pointer moves, in pixels, before a press on a marker is a drag rather than a click.</summary>
    public const float DragThresholdPixels = 6f;

    private enum State
    {
        Idle,
        /// <summary>The button is down on a marker; not yet known whether this is a click or a drag.</summary>
        Pressed,
        Dragging,
    }

    private State _state;
    private int _index = MarkerHitTest.None;
    private float _pressX, _pressY;

    // The last click that removed a marker, which a second click soon after and nearby completes.
    private bool _removed;
    private float _removedAt, _removedX, _removedY;

    /// <summary>
    /// One frame of input. <paramref name="hit"/> is the marker under the pointer, only consulted
    /// when the button went down this frame; <paramref name="onMap"/> says whether the pointer is
    /// over the chart.
    /// </summary>
    public Gesture Frame(float time, bool down, bool held, bool up, float pointerX, float pointerY, int hit, bool onMap)
    {
        switch (_state)
        {
            case State.Pressed:
                if (held && Beyond(DragThresholdPixels, pointerX, pointerY, _pressX, _pressY))
                {
                    _state = State.Dragging;
                    return new Gesture(GestureCommand.Preview, _index);
                }
                if (up || !held)
                {
                    _state = State.Idle;
                    _removed = true;
                    _removedAt = time;
                    _removedX = _pressX;
                    _removedY = _pressY;
                    return new Gesture(GestureCommand.Remove, _index);
                }
                return Gesture.None;

            case State.Dragging:
                if (up || !held)
                {
                    _state = State.Idle;
                    return new Gesture(GestureCommand.Commit, _index);
                }
                return new Gesture(GestureCommand.Preview, _index);

            default:
                if (!down) return Gesture.None;
                if (_removed && time - _removedAt < DoubleClickSeconds && !Beyond(DoubleClickPixels, pointerX, pointerY, _removedX, _removedY))
                {
                    _removed = false;
                    return new Gesture(GestureCommand.Clear, MarkerHitTest.None);
                }
                _removed = false;
                if (hit != MarkerHitTest.None)
                {
                    _state = State.Pressed;
                    _index = hit;
                    _pressX = pointerX;
                    _pressY = pointerY;
                    return Gesture.None;
                }
                return onMap ? new Gesture(GestureCommand.Append, MarkerHitTest.None) : Gesture.None;
        }
    }

    /// <summary>
    /// The chart has closed (or a menu covers it), so nothing more is coming: a drag ends where
    /// it is, and a click just made no longer has a double coming. The command, if any, is to be
    /// carried out.
    /// </summary>
    public Gesture Reset()
    {
        var state = _state;
        var index = _index;
        _state = State.Idle;
        _index = MarkerHitTest.None;
        _removed = false;
        return state == State.Dragging ? new Gesture(GestureCommand.Commit, index) : Gesture.None;
    }

    private static bool Beyond(float pixels, float x, float y, float fromX, float fromY)
    {
        float dx = x - fromX, dy = y - fromY;
        return dx * dx + dy * dy > pixels * pixels;
    }
}
