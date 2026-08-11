using System;

namespace BedrockProtocol.Enums;

public enum FloatAttributeOperation {
    OVERRIDE = 0,
    ALPHA_BLEND = 1,
    ADD = 2,
    SUBTRACT = 3,
    MULTIPLY = 4,
    MINIMUM = 5,
    MAXIMUM = 6,
}

public static class FloatAttributeOperationExtensions {
    public static string ToProtoString(this FloatAttributeOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this FloatAttributeOperation value) {
        return value switch {
            FloatAttributeOperation.OVERRIDE => "OVERRIDE",
            FloatAttributeOperation.ALPHA_BLEND => "ALPHA_BLEND",
            FloatAttributeOperation.ADD => "ADD",
            FloatAttributeOperation.SUBTRACT => "SUBTRACT",
            FloatAttributeOperation.MULTIPLY => "MULTIPLY",
            FloatAttributeOperation.MINIMUM => "MINIMUM",
            FloatAttributeOperation.MAXIMUM => "MAXIMUM",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown FloatAttributeOperation value.")
        };
    }

    public static FloatAttributeOperation FromProtocolString(string value) {
        return value switch {
            "OVERRIDE" => FloatAttributeOperation.OVERRIDE,
            "ALPHA_BLEND" => FloatAttributeOperation.ALPHA_BLEND,
            "ADD" => FloatAttributeOperation.ADD,
            "SUBTRACT" => FloatAttributeOperation.SUBTRACT,
            "MULTIPLY" => FloatAttributeOperation.MULTIPLY,
            "MINIMUM" => FloatAttributeOperation.MINIMUM,
            "MAXIMUM" => FloatAttributeOperation.MAXIMUM,
            _ => throw new ArgumentException($"Unknown FloatAttributeOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out FloatAttributeOperation result) {
        switch (value) {
            case "OVERRIDE":
                result = FloatAttributeOperation.OVERRIDE;
                return true;
            case "ALPHA_BLEND":
                result = FloatAttributeOperation.ALPHA_BLEND;
                return true;
            case "ADD":
                result = FloatAttributeOperation.ADD;
                return true;
            case "SUBTRACT":
                result = FloatAttributeOperation.SUBTRACT;
                return true;
            case "MULTIPLY":
                result = FloatAttributeOperation.MULTIPLY;
                return true;
            case "MINIMUM":
                result = FloatAttributeOperation.MINIMUM;
                return true;
            case "MAXIMUM":
                result = FloatAttributeOperation.MAXIMUM;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
