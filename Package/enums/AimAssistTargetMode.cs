#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum AimAssistTargetMode {
    angle = 0,
    distance = 1,
}

public static class AimAssistTargetModeExtensions {
    public static string ToProtoString(this AimAssistTargetMode value) => value.ToProtocolString();

    public static string ToProtocolString(this AimAssistTargetMode value) {
        return value switch {
            AimAssistTargetMode.angle => "angle",
            AimAssistTargetMode.distance => "distance",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AimAssistTargetMode value.")
        };
    }

    public static AimAssistTargetMode FromProtocolString(string value) {
        return value switch {
            "angle" => AimAssistTargetMode.angle,
            "distance" => AimAssistTargetMode.distance,
            _ => throw new ArgumentException($"Unknown AimAssistTargetMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AimAssistTargetMode result) {
        switch (value) {
            case "angle":
                result = AimAssistTargetMode.angle;
                return true;
            case "distance":
                result = AimAssistTargetMode.distance;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
