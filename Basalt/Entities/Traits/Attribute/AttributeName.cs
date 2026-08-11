namespace Basalt.Core.Entities.Traits.Attribute;

public enum AttributeName : byte {
    Unknown = 0,
    Absorption = 1,
    AttackDamage = 2,
    FallDamage = 3,
    FollowRange = 4,
    Health = 5,
    HorseJumpStrength = 6,
    KnockbackResistance = 7,
    KnockbackResistence = KnockbackResistance,
    LavaMovement = 8,
    Luck = 9,
    Movement = 10,
    PlayerExhaustion = 11,
    PlayerExperience = 12,
    PlayerHunger = 13,
    PlayerLevel = 14,
    PlayerSaturation = 15,
    UnderwaterMovement = 16,
    ZombieSpawnReinforcements = 17,
}

public static class AttributeNameExtensions {
    public static string ToProtocolString(this AttributeName name) => name switch {
        AttributeName.Absorption => "minecraft:absorption",
        AttributeName.AttackDamage => "minecraft:attack_damage",
        AttributeName.FallDamage => "minecraft:fall_damage",
        AttributeName.FollowRange => "minecraft:follow_range",
        AttributeName.Health => "minecraft:health",
        AttributeName.HorseJumpStrength => "minecraft:horse.jump_strength",
        AttributeName.KnockbackResistance => "minecraft:knockback_resistance",
        AttributeName.LavaMovement => "minecraft:lava_movement",
        AttributeName.Luck => "minecraft:luck",
        AttributeName.Movement => "minecraft:movement",
        AttributeName.PlayerExhaustion => "minecraft:player.exhaustion",
        AttributeName.PlayerExperience => "minecraft:player.experience",
        AttributeName.PlayerHunger => "minecraft:player.hunger",
        AttributeName.PlayerLevel => "minecraft:player.level",
        AttributeName.PlayerSaturation => "minecraft:player.saturation",
        AttributeName.UnderwaterMovement => "minecraft:underwater_movement",
        AttributeName.ZombieSpawnReinforcements => "minecraft:zombie.spawn_reinforcements",
        _ => "minecraft:unknown",
    };

    public static AttributeName FromProtocolString(string name) => name switch {
        "minecraft:absorption" => AttributeName.Absorption,
        "minecraft:attack_damage" => AttributeName.AttackDamage,
        "minecraft:fall_damage" => AttributeName.FallDamage,
        "minecraft:follow_range" => AttributeName.FollowRange,
        "minecraft:health" => AttributeName.Health,
        "minecraft:horse.jump_strength" => AttributeName.HorseJumpStrength,
        "minecraft:knockback_resistance" => AttributeName.KnockbackResistance,
        "minecraft:lava_movement" => AttributeName.LavaMovement,
        "minecraft:luck" => AttributeName.Luck,
        "minecraft:movement" => AttributeName.Movement,
        "minecraft:player.exhaustion" => AttributeName.PlayerExhaustion,
        "minecraft:player.experience" => AttributeName.PlayerExperience,
        "minecraft:player.hunger" => AttributeName.PlayerHunger,
        "minecraft:player.level" => AttributeName.PlayerLevel,
        "minecraft:player.saturation" => AttributeName.PlayerSaturation,
        "minecraft:underwater_movement" => AttributeName.UnderwaterMovement,
        "minecraft:zombie.spawn_reinforcements" => AttributeName.ZombieSpawnReinforcements,
        _ => AttributeName.Unknown,
    };

    public static bool TryFromProtocolString(
        string name,
        out AttributeName attribute
    ) {
        attribute = FromProtocolString(name);
        return attribute != AttributeName.Unknown;
    }
}