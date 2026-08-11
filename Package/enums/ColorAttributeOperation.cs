using System;

namespace BedrockProtocol.Enums;

public enum ColorAttributeOperation {
    OVERRIDE = 0,
    ALPHA_BLEND = 1,
    ADD = 2,
    SUBTRACT = 3,
    MULTIPLY = 4,
}

public static class ColorAttributeOperationExtensions {
    public static string ToProtoString(this ColorAttributeOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this ColorAttributeOperation value) {
        return value switch {
            ColorAttributeOperation.OVERRIDE => "OVERRIDE",
            ColorAttributeOperation.ALPHA_BLEND => "ALPHA_BLEND",
            ColorAttributeOperation.ADD => "ADD",
            ColorAttributeOperation.SUBTRACT => "SUBTRACT",
            ColorAttributeOperation.MULTIPLY => "MULTIPLY",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ColorAttributeOperation value.")
        };
    }

    public static ColorAttributeOperation FromProtocolString(string value) {
        return value switch {
            "OVERRIDE" => ColorAttributeOperation.OVERRIDE,
            "ALPHA_BLEND" => ColorAttributeOperation.ALPHA_BLEND,
            "ADD" => ColorAttributeOperation.ADD,
            "SUBTRACT" => ColorAttributeOperation.SUBTRACT,
            "MULTIPLY" => ColorAttributeOperation.MULTIPLY,
            _ => throw new ArgumentException($"Unknown ColorAttributeOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ColorAttributeOperation result) {
        switch (value) {
            case "OVERRIDE":
                result = ColorAttributeOperation.OVERRIDE;
                return true;
            case "ALPHA_BLEND":
                result = ColorAttributeOperation.ALPHA_BLEND;
                return true;
            case "ADD":
                result = ColorAttributeOperation.ADD;
                return true;
            case "SUBTRACT":
                result = ColorAttributeOperation.SUBTRACT;
                return true;
            case "MULTIPLY":
                result = ColorAttributeOperation.MULTIPLY;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
