namespace MedievalWarSim.Core;

public class Camera
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Zoom { get; set; } = 1f;
    public const float MinZoom = 0.25f;
    public const float MaxZoom = 4f;
    public const float PanSpeed = 400f;

    public (float x, float y) ScreenToWorld(float sx, float sy)
        => (sx / Zoom + X, sy / Zoom + Y);

    public (float x, float y) WorldToScreen(float wx, float wy)
        => ((wx - X) * Zoom, (wy - Y) * Zoom);
}
