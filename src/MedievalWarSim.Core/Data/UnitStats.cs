using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Core.Data;

public record UnitStatData
{
    public required float BaseSpeed { get; init; }
    public required float BaseRadius { get; init; }
}

public static class UnitStats
{
    private static readonly Dictionary<UnitType, UnitStatData> _stats = new()
    {
        [UnitType.Infantry] = new() { BaseSpeed = 100f, BaseRadius = 16f },
        [UnitType.Archer]   = new() { BaseSpeed = 95f,  BaseRadius = 14f },
        [UnitType.Cavalry]  = new() { BaseSpeed = 175f, BaseRadius = 16f },
        [UnitType.Ballista] = new() { BaseSpeed = 50f,  BaseRadius = 20f },
        [UnitType.Medic]    = new() { BaseSpeed = 100f, BaseRadius = 14f },
    };

    public static float GetBaseSpeed(UnitType type)
        => _stats.TryGetValue(type, out var data) ? data.BaseSpeed : 100f;

    public static float GetBaseRadius(UnitType type)
        => _stats.TryGetValue(type, out var data) ? data.BaseRadius : 16f;

    public static float RollSpeed(UnitType type)
    {
        float baseSpeed = GetBaseSpeed(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseSpeed * variance;
    }
}
