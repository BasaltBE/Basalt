#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum MovementEffectType {
    GLIDE_BOOST = 0,
    DOLPHIN_BOOST = 1,
    GEYSER_BOOST = 2,
}

public static class MovementEffectTypeExtensions {
    public static string ToProtoString(this MovementEffectType value) => value.ToProtocolString();

    public static string ToProtocolString(this MovementEffectType value) {
        return value switch {
            MovementEffectType.GLIDE_BOOST => "GLIDE_BOOST",
            MovementEffectType.DOLPHIN_BOOST => "DOLPHIN_BOOST",
            MovementEffectType.GEYSER_BOOST => "GEYSER_BOOST",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MovementEffectType value.")
        };
    }

    public static MovementEffectType FromProtocolString(string value) {
        return value switch {
            "GLIDE_BOOST" => MovementEffectType.GLIDE_BOOST,
            "DOLPHIN_BOOST" => MovementEffectType.DOLPHIN_BOOST,
            "GEYSER_BOOST" => MovementEffectType.GEYSER_BOOST,
            _ => throw new ArgumentException($"Unknown MovementEffectType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MovementEffectType result) {
        switch (value) {
            case "GLIDE_BOOST":
                result = MovementEffectType.GLIDE_BOOST;
                return true;
            case "DOLPHIN_BOOST":
                result = MovementEffectType.DOLPHIN_BOOST;
                return true;
            case "GEYSER_BOOST":
                result = MovementEffectType.GEYSER_BOOST;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
