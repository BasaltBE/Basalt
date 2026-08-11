using System;

namespace BedrockProtocol.Enums;

public enum BoolAttributeOperation {
    OVERRIDE = 0,
    ALPHA_BLEND = 1,
    AND = 2,
    NAND = 3,
    OR = 4,
    NOR = 5,
    XOR = 6,
    XNOR = 7,
}

public static class BoolAttributeOperationExtensions {
    public static string ToProtoString(this BoolAttributeOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this BoolAttributeOperation value) {
        return value switch {
            BoolAttributeOperation.OVERRIDE => "OVERRIDE",
            BoolAttributeOperation.ALPHA_BLEND => "ALPHA_BLEND",
            BoolAttributeOperation.AND => "AND",
            BoolAttributeOperation.NAND => "NAND",
            BoolAttributeOperation.OR => "OR",
            BoolAttributeOperation.NOR => "NOR",
            BoolAttributeOperation.XOR => "XOR",
            BoolAttributeOperation.XNOR => "XNOR",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BoolAttributeOperation value.")
        };
    }

    public static BoolAttributeOperation FromProtocolString(string value) {
        return value switch {
            "OVERRIDE" => BoolAttributeOperation.OVERRIDE,
            "ALPHA_BLEND" => BoolAttributeOperation.ALPHA_BLEND,
            "AND" => BoolAttributeOperation.AND,
            "NAND" => BoolAttributeOperation.NAND,
            "OR" => BoolAttributeOperation.OR,
            "NOR" => BoolAttributeOperation.NOR,
            "XOR" => BoolAttributeOperation.XOR,
            "XNOR" => BoolAttributeOperation.XNOR,
            _ => throw new ArgumentException($"Unknown BoolAttributeOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BoolAttributeOperation result) {
        switch (value) {
            case "OVERRIDE":
                result = BoolAttributeOperation.OVERRIDE;
                return true;
            case "ALPHA_BLEND":
                result = BoolAttributeOperation.ALPHA_BLEND;
                return true;
            case "AND":
                result = BoolAttributeOperation.AND;
                return true;
            case "NAND":
                result = BoolAttributeOperation.NAND;
                return true;
            case "OR":
                result = BoolAttributeOperation.OR;
                return true;
            case "NOR":
                result = BoolAttributeOperation.NOR;
                return true;
            case "XOR":
                result = BoolAttributeOperation.XOR;
                return true;
            case "XNOR":
                result = BoolAttributeOperation.XNOR;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
