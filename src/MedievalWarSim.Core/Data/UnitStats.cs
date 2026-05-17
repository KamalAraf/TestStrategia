using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Core.Data;

public record struct UnitStatData
{
    public required float BaseSpeed { get; init; }
    public required float BaseRadius { get; init; }
    public required float BaseHP { get; init; }
}

public static class UnitStats
{
    private static readonly UnitStatData[] _stats =
    [
        new() { BaseSpeed = 100f, BaseRadius = 16f, BaseHP = 100f }, // Infantry
        new() { BaseSpeed = 95f,  BaseRadius = 14f, BaseHP = 60f },  // Archer
        new() { BaseSpeed = 175f, BaseRadius = 16f, BaseHP = 80f },  // Cavalry
        new() { BaseSpeed = 50f,  BaseRadius = 20f, BaseHP = 125f }, // Ballista
        new() { BaseSpeed = 100f, BaseRadius = 14f, BaseHP = 75f },  // Medic
    ];

    public static float GetBaseSpeed(UnitType type) => _stats[(int)type].BaseSpeed;
    public static float GetBaseRadius(UnitType type) => _stats[(int)type].BaseRadius;
    public static float GetBaseHP(UnitType type) => _stats[(int)type].BaseHP;

    public static float RollSpeed(UnitType type)
    {
        float baseVal = GetBaseSpeed(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseVal * variance;
    }

    public static float RollHP(UnitType type)
    {
        float baseVal = GetBaseHP(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseVal * variance;
    }
}
