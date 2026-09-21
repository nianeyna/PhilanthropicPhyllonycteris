namespace PhilanthropicPhyllonycteris.Core;

/// <summary>
/// Whether a click lands on the marker. Compared in screen pixels, since the game keeps chart
/// icons the same size on screen at every zoom.
/// </summary>
public static class MarkerHitTest
{
    public static bool Hits(float clickX, float clickY, float markerX, float markerY, float radiusPixels)
    {
        float dx = clickX - markerX, dy = clickY - markerY;
        return dx * dx + dy * dy <= radiusPixels * radiusPixels;
    }
}
