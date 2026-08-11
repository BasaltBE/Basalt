using System;

namespace BedrockProtocol.Enums;

public enum LabTableReactionType {
    None = 0,
    IceBomb = 1,
    Bleach = 2,
    ElephantToothpaste = 3,
    Fertilizer = 4,
    HeatBlock = 5,
    MagnesiumSalts = 6,
    MiscFire = 7,
    MiscExplosion = 8,
    MiscLava = 9,
    MiscMystical = 10,
    MiscSmoke = 11,
    MiscLargeSmoke = 12,
}

public static class LabTableReactionTypeExtensions {
    public static string ToProtoString(this LabTableReactionType value) => value.ToProtocolString();

    public static string ToProtocolString(this LabTableReactionType value) {
        return value switch {
            LabTableReactionType.None => "None",
            LabTableReactionType.IceBomb => "IceBomb",
            LabTableReactionType.Bleach => "Bleach",
            LabTableReactionType.ElephantToothpaste => "ElephantToothpaste",
            LabTableReactionType.Fertilizer => "Fertilizer",
            LabTableReactionType.HeatBlock => "HeatBlock",
            LabTableReactionType.MagnesiumSalts => "MagnesiumSalts",
            LabTableReactionType.MiscFire => "MiscFire",
            LabTableReactionType.MiscExplosion => "MiscExplosion",
            LabTableReactionType.MiscLava => "MiscLava",
            LabTableReactionType.MiscMystical => "MiscMystical",
            LabTableReactionType.MiscSmoke => "MiscSmoke",
            LabTableReactionType.MiscLargeSmoke => "MiscLargeSmoke",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LabTableReactionType value.")
        };
    }

    public static LabTableReactionType FromProtocolString(string value) {
        return value switch {
            "None" => LabTableReactionType.None,
            "IceBomb" => LabTableReactionType.IceBomb,
            "Bleach" => LabTableReactionType.Bleach,
            "ElephantToothpaste" => LabTableReactionType.ElephantToothpaste,
            "Fertilizer" => LabTableReactionType.Fertilizer,
            "HeatBlock" => LabTableReactionType.HeatBlock,
            "MagnesiumSalts" => LabTableReactionType.MagnesiumSalts,
            "MiscFire" => LabTableReactionType.MiscFire,
            "MiscExplosion" => LabTableReactionType.MiscExplosion,
            "MiscLava" => LabTableReactionType.MiscLava,
            "MiscMystical" => LabTableReactionType.MiscMystical,
            "MiscSmoke" => LabTableReactionType.MiscSmoke,
            "MiscLargeSmoke" => LabTableReactionType.MiscLargeSmoke,
            _ => throw new ArgumentException($"Unknown LabTableReactionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out LabTableReactionType result) {
        switch (value) {
            case "None":
                result = LabTableReactionType.None;
                return true;
            case "IceBomb":
                result = LabTableReactionType.IceBomb;
                return true;
            case "Bleach":
                result = LabTableReactionType.Bleach;
                return true;
            case "ElephantToothpaste":
                result = LabTableReactionType.ElephantToothpaste;
                return true;
            case "Fertilizer":
                result = LabTableReactionType.Fertilizer;
                return true;
            case "HeatBlock":
                result = LabTableReactionType.HeatBlock;
                return true;
            case "MagnesiumSalts":
                result = LabTableReactionType.MagnesiumSalts;
                return true;
            case "MiscFire":
                result = LabTableReactionType.MiscFire;
                return true;
            case "MiscExplosion":
                result = LabTableReactionType.MiscExplosion;
                return true;
            case "MiscLava":
                result = LabTableReactionType.MiscLava;
                return true;
            case "MiscMystical":
                result = LabTableReactionType.MiscMystical;
                return true;
            case "MiscSmoke":
                result = LabTableReactionType.MiscSmoke;
                return true;
            case "MiscLargeSmoke":
                result = LabTableReactionType.MiscLargeSmoke;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
