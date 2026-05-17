using MedievalWarSim.Core.Components;

namespace MedievalWarSim.Core.Managers;

public class EntityManager
{
    private const int MAX_ENTITIES = 2000;

    private readonly PositionComponent[] _positions;
    private readonly UnitTypeComponent[]  _unitTypes;
    private readonly MoveComponent[] _moves;
    private readonly bool[]               _alive;

    private readonly Stack<int> _freeSlots = new();
    private int _nextNew = 0;
    private int _count;

    public EntityManager()
    {
        _positions = new PositionComponent[MAX_ENTITIES];
        _unitTypes = new UnitTypeComponent[MAX_ENTITIES];
        _moves = new MoveComponent[MAX_ENTITIES];
        _alive     = new bool[MAX_ENTITIES];
    }

    public int Create()
    {
        int slot;
        if (_freeSlots.Count > 0)
            slot = _freeSlots.Pop();
        else if (_nextNew < MAX_ENTITIES)
            slot = _nextNew++;
        else
            return -1;

        _alive[slot] = true;
        _count++;
        return slot;
    }

    public void Destroy(int entityId)
    {
        if (!IsAlive(entityId)) return;
        _alive[entityId] = false;
        _count--;
        _freeSlots.Push(entityId);
    }

    public int  Count         => _count;
    public int  Max           => MAX_ENTITIES;
    public int  HighWaterMark => _nextNew;

    public ref PositionComponent GetPosition(int entityId)
    {
        if ((uint)entityId >= (uint)MAX_ENTITIES)
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Invalid entity ID");
        return ref _positions[entityId];
    }

    public ref UnitTypeComponent GetUnitType(int entityId)
    {
        if ((uint)entityId >= (uint)MAX_ENTITIES)
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Invalid entity ID");
        return ref _unitTypes[entityId];
    }

    public ref MoveComponent GetMove(int entityId)
    {
        if ((uint)entityId >= (uint)MAX_ENTITIES)
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Invalid entity ID");
        return ref _moves[entityId];
    }

    public bool IsAlive(int entityId)
        => (uint)entityId < (uint)MAX_ENTITIES && _alive[entityId];

    public ReadOnlySpan<bool> Alive => _alive.AsSpan(0, _nextNew);
}
