namespace Basalt.Protocol.Enums;

public static class AttributeNameHelper {
    public static AttributeName FromProtocolString(string value) => value switch {
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
        _ => AttributeName.Unknown
    };
}
