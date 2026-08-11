using System;

namespace BedrockProtocol.Enums;

public enum AttributeOperands {
    OPERAND_MIN = 0,
    OPERAND_MAX = 1,
    OPERAND_CURRENT = 2,
    TOTAL_OPERANDS = 3,
    OPERAND_INVALID = 3,
}

public static class AttributeOperandsExtensions {
    public static string ToProtoString(this AttributeOperands value) => value.ToProtocolString();

    public static string ToProtocolString(this AttributeOperands value) {
        return value switch {
            AttributeOperands.OPERAND_MIN => "OPERAND_MIN",
            AttributeOperands.OPERAND_MAX => "OPERAND_MAX",
            AttributeOperands.OPERAND_CURRENT => "OPERAND_CURRENT",
            AttributeOperands.TOTAL_OPERANDS => "TOTAL_OPERANDS",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AttributeOperands value.")
        };
    }

    public static AttributeOperands FromProtocolString(string value) {
        return value switch {
            "OPERAND_MIN" => AttributeOperands.OPERAND_MIN,
            "OPERAND_MAX" => AttributeOperands.OPERAND_MAX,
            "OPERAND_CURRENT" => AttributeOperands.OPERAND_CURRENT,
            "TOTAL_OPERANDS" => AttributeOperands.TOTAL_OPERANDS,
            "OPERAND_INVALID" => AttributeOperands.OPERAND_INVALID,
            _ => throw new ArgumentException($"Unknown AttributeOperands protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AttributeOperands result) {
        switch (value) {
            case "OPERAND_MIN":
                result = AttributeOperands.OPERAND_MIN;
                return true;
            case "OPERAND_MAX":
                result = AttributeOperands.OPERAND_MAX;
                return true;
            case "OPERAND_CURRENT":
                result = AttributeOperands.OPERAND_CURRENT;
                return true;
            case "TOTAL_OPERANDS":
                result = AttributeOperands.TOTAL_OPERANDS;
                return true;
            case "OPERAND_INVALID":
                result = AttributeOperands.OPERAND_INVALID;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
