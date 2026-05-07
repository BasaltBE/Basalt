namespace Basalt.Protocol.Enums;

public static class AttributeNameHelper
{
    public static AttributeName FromProtocolString(string value) => value switch
    {
        "minecraft:health" => AttributeName.Health,
        "minecraft:movement" => AttributeName.Movement,
        "minecraft:absorption" => AttributeName.Absorption,
        "minecraft:player.hunger" => AttributeName.Hunger,
        "minecraft:player.saturation" => AttributeName.Saturation,
        "minecraft:player.exhaustion" => AttributeName.Exhaustion,
        "minecraft:attack_damage" => AttributeName.AttackDamage,
        "minecraft:knockback_resistance" => AttributeName.KnockbackResistance,
        _ => AttributeName.Unknown
    };
}
