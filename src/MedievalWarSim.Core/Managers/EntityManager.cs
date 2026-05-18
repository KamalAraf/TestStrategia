using MedievalWarSim.Core.Components;

namespace MedievalWarSim.Core.Managers;

public class EntityManager
{
    private const int MAX_ENTITIES = 100000;

    private readonly PositionComponent[] _positions;
    private readonly UnitTypeComponent[]  _unitTypes;
    private readonly MoveComponent[] _moves;
    private readonly HealthComponent[] _health;
    private readonly VisionComponent[] _visions;
    private readonly bool[]           _alive;

    private readonly Stack<int> _freeSlots = new();
    private readonly int[]      _activeEntities;
    private readonly int[]      _entityToIndex; // Maps entityId -> index in _activeEntities
    private int _nextNew = 0;
    private int _count;

    public EntityManager()
    {
        _positions = new PositionComponent[MAX_ENTITIES];
        _unitTypes = new UnitTypeComponent[MAX_ENTITIES];
        _moves = new MoveComponent[MAX_ENTITIES];
        _health = new HealthComponent[MAX_ENTITIES];
        _visions = new VisionComponent[MAX_ENTITIES];
        _alive = new bool[MAX_ENTITIES];
        _activeEntities = new int[MAX_ENTITIES];
        _entityToIndex = new int[MAX_ENTITIES];
        Array.Fill(_entityToIndex, -1);
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
        _entityToIndex[slot] = _count;
        _activeEntities[_count] = slot;
        _count++;
        return slot;
    }

    public void Destroy(int entityId)
    {
        if (!IsAlive(entityId)) return;

        int index = _entityToIndex[entityId];
        int lastEntityId = _activeEntities[_count - 1];

        // Swap with last
        _activeEntities[index] = lastEntityId;
        _entityToIndex[lastEntityId] = index;

        _entityToIndex[entityId] = -1;
        _alive[entityId] = false;
        _count--;
        _freeSlots.Push(entityId);
    }

    public int  Count         => _count;
    public int  Max           => MAX_ENTITIES;
    public int  HighWaterMark => _nextNew;

    public ReadOnlySpan<int> ActiveEntities => _activeEntities.AsSpan(0, _count);

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

    public ref HealthComponent GetHealth(int entityId)
    {
        if ((uint)entityId >= (uint)MAX_ENTITIES)
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Invalid entity ID");
        return ref _health[entityId];
    }

    public ref VisionComponent GetVision(int entityId)
    {
        if ((uint)entityId >= (uint)MAX_ENTITIES)
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Invalid entity ID");
        return ref _visions[entityId];
    }

    public bool IsAlive(int entityId)
        => (uint)entityId < (uint)MAX_ENTITIES && _alive[entityId];

    public ReadOnlySpan<bool> Alive => _alive.AsSpan(0, _nextNew);
}
