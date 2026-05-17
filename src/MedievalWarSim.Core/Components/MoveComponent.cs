namespace MedievalWarSim.Core.Components;

public struct MoveComponent
{
    public float TargetX;
    public float TargetY;
    public float Speed;
    public bool IsMoving;
    public float FacingAngle; // radians, 0 = up, π/2 = right
}
