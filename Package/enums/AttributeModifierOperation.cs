using System;

namespace BedrockProtocol.Enums;

public enum AttributeModifierOperation {
    OPERATION_ADDITION = 0,
    OPERATION_MULTIPLY_BASE = 1,
    OPERATION_MULTIPLY_TOTAL = 2,
    OPERATION_CAP = 3,
    TOTAL_OPERATIONS = 4,
    OPERATION_INVALID = 4,
}

public static class AttributeModifierOperationExtensions {
    public static string ToProtoString(this AttributeModifierOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this AttributeModifierOperation value) {
        return value switch {
            AttributeModifierOperation.OPERATION_ADDITION => "OPERATION_ADDITION",
            AttributeModifierOperation.OPERATION_MULTIPLY_BASE => "OPERATION_MULTIPLY_BASE",
            AttributeModifierOperation.OPERATION_MULTIPLY_TOTAL => "OPERATION_MULTIPLY_TOTAL",
            AttributeModifierOperation.OPERATION_CAP => "OPERATION_CAP",
            AttributeModifierOperation.TOTAL_OPERATIONS => "TOTAL_OPERATIONS",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AttributeModifierOperation value.")
        };
    }

    public static AttributeModifierOperation FromProtocolString(string value) {
        return value switch {
            "OPERATION_ADDITION" => AttributeModifierOperation.OPERATION_ADDITION,
            "OPERATION_MULTIPLY_BASE" => AttributeModifierOperation.OPERATION_MULTIPLY_BASE,
            "OPERATION_MULTIPLY_TOTAL" => AttributeModifierOperation.OPERATION_MULTIPLY_TOTAL,
            "OPERATION_CAP" => AttributeModifierOperation.OPERATION_CAP,
            "TOTAL_OPERATIONS" => AttributeModifierOperation.TOTAL_OPERATIONS,
            "OPERATION_INVALID" => AttributeModifierOperation.OPERATION_INVALID,
            _ => throw new ArgumentException($"Unknown AttributeModifierOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AttributeModifierOperation result) {
        switch (value) {
            case "OPERATION_ADDITION":
                result = AttributeModifierOperation.OPERATION_ADDITION;
                return true;
            case "OPERATION_MULTIPLY_BASE":
                result = AttributeModifierOperation.OPERATION_MULTIPLY_BASE;
                return true;
            case "OPERATION_MULTIPLY_TOTAL":
                result = AttributeModifierOperation.OPERATION_MULTIPLY_TOTAL;
                return true;
            case "OPERATION_CAP":
                result = AttributeModifierOperation.OPERATION_CAP;
                return true;
            case "TOTAL_OPERATIONS":
                result = AttributeModifierOperation.TOTAL_OPERATIONS;
                return true;
            case "OPERATION_INVALID":
                result = AttributeModifierOperation.OPERATION_INVALID;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
