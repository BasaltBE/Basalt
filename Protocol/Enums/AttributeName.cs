namespace Basalt.Protocol.Enums;

public enum AttributeName : byte
{
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

public static class AttributeNameExtensions
{
    public static string ToProtocolString(this AttributeName name) => name switch
    {
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
}
