using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Core.Data;

public record struct UnitStatData
{
    public required float BaseSpeed { get; init; }
    public required float BaseRadius { get; init; }
    public required float BaseHP { get; init; }
    public required float BaseSightRange { get; init; }
    public required float BaseMaxStamina { get; init; }
}

public static class UnitStats
{
    private static readonly UnitStatData[] _stats =
    [
        new() { BaseSpeed = 200f, BaseRadius = 30f, BaseHP = 100f, BaseSightRange = 1500f, BaseMaxStamina = 100f }, // Infantry
        new() { BaseSpeed = 180f, BaseRadius = 28f, BaseHP = 60f,  BaseSightRange = 2250f, BaseMaxStamina = 100f }, // Archer
        new() { BaseSpeed = 600f, BaseRadius = 40f, BaseHP = 130f, BaseSightRange = 3000f, BaseMaxStamina = 100f }, // Cavalry
        new() { BaseSpeed = 60f,  BaseRadius = 55f, BaseHP = 150f, BaseSightRange = 2250f, BaseMaxStamina = 100f }, // Ballista
        new() { BaseSpeed = 240f, BaseRadius = 28f, BaseHP = 80f,  BaseSightRange = 1500f, BaseMaxStamina = 100f }, // Medic
    ];

    public static float GetBaseSpeed(UnitType type) => _stats[(int)type].BaseSpeed;
    public static float GetBaseRadius(UnitType type) => _stats[(int)type].BaseRadius;
    public static float GetBaseHP(UnitType type) => _stats[(int)type].BaseHP;
    public static float GetBaseSightRange(UnitType type) => _stats[(int)type].BaseSightRange;
    public static float GetBaseMaxStamina(UnitType type) => _stats[(int)type].BaseMaxStamina;

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

    public static float RollSightRange(UnitType type)
    {
        float baseVal = GetBaseSightRange(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseVal * variance;
    }

    public static float RollMaxStamina(UnitType type)
    {
        float baseVal = GetBaseMaxStamina(type);
        float variance = 1f + (Random.Shared.NextSingle() * 0.1f - 0.05f);
        return baseVal * variance;
    }
}
