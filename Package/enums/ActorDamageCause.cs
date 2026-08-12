#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ActorDamageCause {
    Override = 0,
    Contact = 1,
    EntityAttack = 2,
    Projectile = 3,
    Suffocation = 4,
    Fall = 5,
    Fire = 6,
    FireTick = 7,
    Lava = 8,
    Drowning = 9,
    BlockExplosion = 10,
    EntityExplosion = 11,
    Void = 12,
    SelfDestruct = 13,
    Magic = 14,
    Wither = 15,
    Starve = 16,
    Anvil = 17,
    Thorns = 18,
    FallingBlock = 19,
    Piston = 20,
    FlyIntoWall = 21,
    Magma = 22,
    Fireworks = 23,
    Lightning = 24,
    Charging = 25,
    Temperature = 26,
    Freezing = 27,
    Stalactite = 28,
    Stalagmite = 29,
    RamAttack = 30,
    SonicBoom = 31,
    Campfire = 32,
    SoulCampfire = 33,
    MaceSmash = 34,
}

public static class ActorDamageCauseExtensions {
    public static string ToProtoString(this ActorDamageCause value) => value.ToProtocolString();

    public static string ToProtocolString(this ActorDamageCause value) {
        return value switch {
            ActorDamageCause.Override => "Override",
            ActorDamageCause.Contact => "Contact",
            ActorDamageCause.EntityAttack => "EntityAttack",
            ActorDamageCause.Projectile => "Projectile",
            ActorDamageCause.Suffocation => "Suffocation",
            ActorDamageCause.Fall => "Fall",
            ActorDamageCause.Fire => "Fire",
            ActorDamageCause.FireTick => "FireTick",
            ActorDamageCause.Lava => "Lava",
            ActorDamageCause.Drowning => "Drowning",
            ActorDamageCause.BlockExplosion => "BlockExplosion",
            ActorDamageCause.EntityExplosion => "EntityExplosion",
            ActorDamageCause.Void => "Void",
            ActorDamageCause.SelfDestruct => "SelfDestruct",
            ActorDamageCause.Magic => "Magic",
            ActorDamageCause.Wither => "Wither",
            ActorDamageCause.Starve => "Starve",
            ActorDamageCause.Anvil => "Anvil",
            ActorDamageCause.Thorns => "Thorns",
            ActorDamageCause.FallingBlock => "FallingBlock",
            ActorDamageCause.Piston => "Piston",
            ActorDamageCause.FlyIntoWall => "FlyIntoWall",
            ActorDamageCause.Magma => "Magma",
            ActorDamageCause.Fireworks => "Fireworks",
            ActorDamageCause.Lightning => "Lightning",
            ActorDamageCause.Charging => "Charging",
            ActorDamageCause.Temperature => "Temperature",
            ActorDamageCause.Freezing => "Freezing",
            ActorDamageCause.Stalactite => "Stalactite",
            ActorDamageCause.Stalagmite => "Stalagmite",
            ActorDamageCause.RamAttack => "RamAttack",
            ActorDamageCause.SonicBoom => "SonicBoom",
            ActorDamageCause.Campfire => "Campfire",
            ActorDamageCause.SoulCampfire => "SoulCampfire",
            ActorDamageCause.MaceSmash => "MaceSmash",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ActorDamageCause value.")
        };
    }

    public static ActorDamageCause FromProtocolString(string value) {
        return value switch {
            "Override" => ActorDamageCause.Override,
            "Contact" => ActorDamageCause.Contact,
            "EntityAttack" => ActorDamageCause.EntityAttack,
            "Projectile" => ActorDamageCause.Projectile,
            "Suffocation" => ActorDamageCause.Suffocation,
            "Fall" => ActorDamageCause.Fall,
            "Fire" => ActorDamageCause.Fire,
            "FireTick" => ActorDamageCause.FireTick,
            "Lava" => ActorDamageCause.Lava,
            "Drowning" => ActorDamageCause.Drowning,
            "BlockExplosion" => ActorDamageCause.BlockExplosion,
            "EntityExplosion" => ActorDamageCause.EntityExplosion,
            "Void" => ActorDamageCause.Void,
            "SelfDestruct" => ActorDamageCause.SelfDestruct,
            "Magic" => ActorDamageCause.Magic,
            "Wither" => ActorDamageCause.Wither,
            "Starve" => ActorDamageCause.Starve,
            "Anvil" => ActorDamageCause.Anvil,
            "Thorns" => ActorDamageCause.Thorns,
            "FallingBlock" => ActorDamageCause.FallingBlock,
            "Piston" => ActorDamageCause.Piston,
            "FlyIntoWall" => ActorDamageCause.FlyIntoWall,
            "Magma" => ActorDamageCause.Magma,
            "Fireworks" => ActorDamageCause.Fireworks,
            "Lightning" => ActorDamageCause.Lightning,
            "Charging" => ActorDamageCause.Charging,
            "Temperature" => ActorDamageCause.Temperature,
            "Freezing" => ActorDamageCause.Freezing,
            "Stalactite" => ActorDamageCause.Stalactite,
            "Stalagmite" => ActorDamageCause.Stalagmite,
            "RamAttack" => ActorDamageCause.RamAttack,
            "SonicBoom" => ActorDamageCause.SonicBoom,
            "Campfire" => ActorDamageCause.Campfire,
            "SoulCampfire" => ActorDamageCause.SoulCampfire,
            "MaceSmash" => ActorDamageCause.MaceSmash,
            _ => throw new ArgumentException($"Unknown ActorDamageCause protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ActorDamageCause result) {
        switch (value) {
            case "Override":
                result = ActorDamageCause.Override;
                return true;
            case "Contact":
                result = ActorDamageCause.Contact;
                return true;
            case "EntityAttack":
                result = ActorDamageCause.EntityAttack;
                return true;
            case "Projectile":
                result = ActorDamageCause.Projectile;
                return true;
            case "Suffocation":
                result = ActorDamageCause.Suffocation;
                return true;
            case "Fall":
                result = ActorDamageCause.Fall;
                return true;
            case "Fire":
                result = ActorDamageCause.Fire;
                return true;
            case "FireTick":
                result = ActorDamageCause.FireTick;
                return true;
            case "Lava":
                result = ActorDamageCause.Lava;
                return true;
            case "Drowning":
                result = ActorDamageCause.Drowning;
                return true;
            case "BlockExplosion":
                result = ActorDamageCause.BlockExplosion;
                return true;
            case "EntityExplosion":
                result = ActorDamageCause.EntityExplosion;
                return true;
            case "Void":
                result = ActorDamageCause.Void;
                return true;
            case "SelfDestruct":
                result = ActorDamageCause.SelfDestruct;
                return true;
            case "Magic":
                result = ActorDamageCause.Magic;
                return true;
            case "Wither":
                result = ActorDamageCause.Wither;
                return true;
            case "Starve":
                result = ActorDamageCause.Starve;
                return true;
            case "Anvil":
                result = ActorDamageCause.Anvil;
                return true;
            case "Thorns":
                result = ActorDamageCause.Thorns;
                return true;
            case "FallingBlock":
                result = ActorDamageCause.FallingBlock;
                return true;
            case "Piston":
                result = ActorDamageCause.Piston;
                return true;
            case "FlyIntoWall":
                result = ActorDamageCause.FlyIntoWall;
                return true;
            case "Magma":
                result = ActorDamageCause.Magma;
                return true;
            case "Fireworks":
                result = ActorDamageCause.Fireworks;
                return true;
            case "Lightning":
                result = ActorDamageCause.Lightning;
                return true;
            case "Charging":
                result = ActorDamageCause.Charging;
                return true;
            case "Temperature":
                result = ActorDamageCause.Temperature;
                return true;
            case "Freezing":
                result = ActorDamageCause.Freezing;
                return true;
            case "Stalactite":
                result = ActorDamageCause.Stalactite;
                return true;
            case "Stalagmite":
                result = ActorDamageCause.Stalagmite;
                return true;
            case "RamAttack":
                result = ActorDamageCause.RamAttack;
                return true;
            case "SonicBoom":
                result = ActorDamageCause.SonicBoom;
                return true;
            case "Campfire":
                result = ActorDamageCause.Campfire;
                return true;
            case "SoulCampfire":
                result = ActorDamageCause.SoulCampfire;
                return true;
            case "MaceSmash":
                result = ActorDamageCause.MaceSmash;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
