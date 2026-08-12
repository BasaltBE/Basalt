#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum EnchantType {
    Protection = 0,
    FireProtection = 1,
    FeatherFalling = 2,
    BlastProtection = 3,
    ProjectileProtection = 4,
    Thorns = 5,
    Respiration = 6,
    DepthStrider = 7,
    AquaAffinity = 8,
    Sharpness = 9,
    Smite = 10,
    BaneOfArthropods = 11,
    Knockback = 12,
    FireAspect = 13,
    Looting = 14,
    Efficiency = 15,
    SilkTouch = 16,
    Unbreaking = 17,
    Fortune = 18,
    Power = 19,
    Punch = 20,
    Flame = 21,
    Infinity = 22,
    LuckOfTheSea = 23,
    Lure = 24,
    FrostWalker = 25,
    Mending = 26,
    CurseOfBinding = 27,
    CurseOfVanishing = 28,
    Impaling = 29,
    Riptide = 30,
    Loyalty = 31,
    Channeling = 32,
    Multishot = 33,
    Piercing = 34,
    QuickCharge = 35,
    SoulSpeed = 36,
    SwiftSneak = 37,
    WindBurst = 38,
    Density = 39,
    Breach = 40,
    Lunge = 41,
    NumEnchantments = 42,
    InvalidEnchantment = 43,
}

public static class EnchantTypeExtensions {
    public static string ToProtoString(this EnchantType value) => value.ToProtocolString();

    public static string ToProtocolString(this EnchantType value) {
        return value switch {
            EnchantType.Protection => "Protection",
            EnchantType.FireProtection => "FireProtection",
            EnchantType.FeatherFalling => "FeatherFalling",
            EnchantType.BlastProtection => "BlastProtection",
            EnchantType.ProjectileProtection => "ProjectileProtection",
            EnchantType.Thorns => "Thorns",
            EnchantType.Respiration => "Respiration",
            EnchantType.DepthStrider => "DepthStrider",
            EnchantType.AquaAffinity => "AquaAffinity",
            EnchantType.Sharpness => "Sharpness",
            EnchantType.Smite => "Smite",
            EnchantType.BaneOfArthropods => "BaneOfArthropods",
            EnchantType.Knockback => "Knockback",
            EnchantType.FireAspect => "FireAspect",
            EnchantType.Looting => "Looting",
            EnchantType.Efficiency => "Efficiency",
            EnchantType.SilkTouch => "SilkTouch",
            EnchantType.Unbreaking => "Unbreaking",
            EnchantType.Fortune => "Fortune",
            EnchantType.Power => "Power",
            EnchantType.Punch => "Punch",
            EnchantType.Flame => "Flame",
            EnchantType.Infinity => "Infinity",
            EnchantType.LuckOfTheSea => "LuckOfTheSea",
            EnchantType.Lure => "Lure",
            EnchantType.FrostWalker => "FrostWalker",
            EnchantType.Mending => "Mending",
            EnchantType.CurseOfBinding => "CurseOfBinding",
            EnchantType.CurseOfVanishing => "CurseOfVanishing",
            EnchantType.Impaling => "Impaling",
            EnchantType.Riptide => "Riptide",
            EnchantType.Loyalty => "Loyalty",
            EnchantType.Channeling => "Channeling",
            EnchantType.Multishot => "Multishot",
            EnchantType.Piercing => "Piercing",
            EnchantType.QuickCharge => "QuickCharge",
            EnchantType.SoulSpeed => "SoulSpeed",
            EnchantType.SwiftSneak => "SwiftSneak",
            EnchantType.WindBurst => "WindBurst",
            EnchantType.Density => "Density",
            EnchantType.Breach => "Breach",
            EnchantType.Lunge => "Lunge",
            EnchantType.NumEnchantments => "NumEnchantments",
            EnchantType.InvalidEnchantment => "InvalidEnchantment",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown EnchantType value.")
        };
    }

    public static EnchantType FromProtocolString(string value) {
        return value switch {
            "Protection" => EnchantType.Protection,
            "FireProtection" => EnchantType.FireProtection,
            "FeatherFalling" => EnchantType.FeatherFalling,
            "BlastProtection" => EnchantType.BlastProtection,
            "ProjectileProtection" => EnchantType.ProjectileProtection,
            "Thorns" => EnchantType.Thorns,
            "Respiration" => EnchantType.Respiration,
            "DepthStrider" => EnchantType.DepthStrider,
            "AquaAffinity" => EnchantType.AquaAffinity,
            "Sharpness" => EnchantType.Sharpness,
            "Smite" => EnchantType.Smite,
            "BaneOfArthropods" => EnchantType.BaneOfArthropods,
            "Knockback" => EnchantType.Knockback,
            "FireAspect" => EnchantType.FireAspect,
            "Looting" => EnchantType.Looting,
            "Efficiency" => EnchantType.Efficiency,
            "SilkTouch" => EnchantType.SilkTouch,
            "Unbreaking" => EnchantType.Unbreaking,
            "Fortune" => EnchantType.Fortune,
            "Power" => EnchantType.Power,
            "Punch" => EnchantType.Punch,
            "Flame" => EnchantType.Flame,
            "Infinity" => EnchantType.Infinity,
            "LuckOfTheSea" => EnchantType.LuckOfTheSea,
            "Lure" => EnchantType.Lure,
            "FrostWalker" => EnchantType.FrostWalker,
            "Mending" => EnchantType.Mending,
            "CurseOfBinding" => EnchantType.CurseOfBinding,
            "CurseOfVanishing" => EnchantType.CurseOfVanishing,
            "Impaling" => EnchantType.Impaling,
            "Riptide" => EnchantType.Riptide,
            "Loyalty" => EnchantType.Loyalty,
            "Channeling" => EnchantType.Channeling,
            "Multishot" => EnchantType.Multishot,
            "Piercing" => EnchantType.Piercing,
            "QuickCharge" => EnchantType.QuickCharge,
            "SoulSpeed" => EnchantType.SoulSpeed,
            "SwiftSneak" => EnchantType.SwiftSneak,
            "WindBurst" => EnchantType.WindBurst,
            "Density" => EnchantType.Density,
            "Breach" => EnchantType.Breach,
            "Lunge" => EnchantType.Lunge,
            "NumEnchantments" => EnchantType.NumEnchantments,
            "InvalidEnchantment" => EnchantType.InvalidEnchantment,
            _ => throw new ArgumentException($"Unknown EnchantType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out EnchantType result) {
        switch (value) {
            case "Protection":
                result = EnchantType.Protection;
                return true;
            case "FireProtection":
                result = EnchantType.FireProtection;
                return true;
            case "FeatherFalling":
                result = EnchantType.FeatherFalling;
                return true;
            case "BlastProtection":
                result = EnchantType.BlastProtection;
                return true;
            case "ProjectileProtection":
                result = EnchantType.ProjectileProtection;
                return true;
            case "Thorns":
                result = EnchantType.Thorns;
                return true;
            case "Respiration":
                result = EnchantType.Respiration;
                return true;
            case "DepthStrider":
                result = EnchantType.DepthStrider;
                return true;
            case "AquaAffinity":
                result = EnchantType.AquaAffinity;
                return true;
            case "Sharpness":
                result = EnchantType.Sharpness;
                return true;
            case "Smite":
                result = EnchantType.Smite;
                return true;
            case "BaneOfArthropods":
                result = EnchantType.BaneOfArthropods;
                return true;
            case "Knockback":
                result = EnchantType.Knockback;
                return true;
            case "FireAspect":
                result = EnchantType.FireAspect;
                return true;
            case "Looting":
                result = EnchantType.Looting;
                return true;
            case "Efficiency":
                result = EnchantType.Efficiency;
                return true;
            case "SilkTouch":
                result = EnchantType.SilkTouch;
                return true;
            case "Unbreaking":
                result = EnchantType.Unbreaking;
                return true;
            case "Fortune":
                result = EnchantType.Fortune;
                return true;
            case "Power":
                result = EnchantType.Power;
                return true;
            case "Punch":
                result = EnchantType.Punch;
                return true;
            case "Flame":
                result = EnchantType.Flame;
                return true;
            case "Infinity":
                result = EnchantType.Infinity;
                return true;
            case "LuckOfTheSea":
                result = EnchantType.LuckOfTheSea;
                return true;
            case "Lure":
                result = EnchantType.Lure;
                return true;
            case "FrostWalker":
                result = EnchantType.FrostWalker;
                return true;
            case "Mending":
                result = EnchantType.Mending;
                return true;
            case "CurseOfBinding":
                result = EnchantType.CurseOfBinding;
                return true;
            case "CurseOfVanishing":
                result = EnchantType.CurseOfVanishing;
                return true;
            case "Impaling":
                result = EnchantType.Impaling;
                return true;
            case "Riptide":
                result = EnchantType.Riptide;
                return true;
            case "Loyalty":
                result = EnchantType.Loyalty;
                return true;
            case "Channeling":
                result = EnchantType.Channeling;
                return true;
            case "Multishot":
                result = EnchantType.Multishot;
                return true;
            case "Piercing":
                result = EnchantType.Piercing;
                return true;
            case "QuickCharge":
                result = EnchantType.QuickCharge;
                return true;
            case "SoulSpeed":
                result = EnchantType.SoulSpeed;
                return true;
            case "SwiftSneak":
                result = EnchantType.SwiftSneak;
                return true;
            case "WindBurst":
                result = EnchantType.WindBurst;
                return true;
            case "Density":
                result = EnchantType.Density;
                return true;
            case "Breach":
                result = EnchantType.Breach;
                return true;
            case "Lunge":
                result = EnchantType.Lunge;
                return true;
            case "NumEnchantments":
                result = EnchantType.NumEnchantments;
                return true;
            case "InvalidEnchantment":
                result = EnchantType.InvalidEnchantment;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
