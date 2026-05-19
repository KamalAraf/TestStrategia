namespace MedievalWarSim.Core.Components;

public struct MoveComponent
{
    public float TargetX;
    public float TargetY;
    public float Speed;
    public bool IsMoving;
    public float FacingAngle;    // radians, 0 = up, π/2 = right
    public float PrevTargetX;    // for facing angle change detection
    public float PrevTargetY;
    public float StuckTimer;     // seconds of near-zero progress
    public float DistCheckTimer; // counts up to 0.5s then checks net progress
    public float PrevDist;       // distance at last check
}
