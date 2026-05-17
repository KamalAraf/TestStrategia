using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Core.Data;

public record UnitStatData
{
    public required float BaseSpeed { get; init; }
}

public static class UnitStats
{
    private static readonly Dictionary<UnitType, UnitStatData> _stats = new()
    {
        [UnitType.Infantry] = new() { BaseSpeed = 100f },
        [UnitType.Archer]   = new() { BaseSpeed = 95f },
        [UnitType.Cavalry]  = new() { BaseSpeed = 175f },
        [UnitType.Ballista] = new() { BaseSpeed = 50f },
        [UnitType.Medic]    = new() { BaseSpeed = 100f },
    };

    public static float GetBaseSpeed(UnitType type)
        => _stats.TryGetValue(type, out var data) ? data.BaseSpeed : 100f;

    public static float RollSpeed(UnitType type)
    {
        float baseSpeed = GetBaseSpeed(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseSpeed * variance;
    }
}
