using System;

namespace BedrockProtocol.Enums;

public enum CameraShakeType {
    Positional = 0,
    Rotational = 1,
}

public static class CameraShakeTypeExtensions {
    public static string ToProtoString(this CameraShakeType value) => value.ToProtocolString();

    public static string ToProtocolString(this CameraShakeType value) {
        return value switch {
            CameraShakeType.Positional => "Positional",
            CameraShakeType.Rotational => "Rotational",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CameraShakeType value.")
        };
    }

    public static CameraShakeType FromProtocolString(string value) {
        return value switch {
            "Positional" => CameraShakeType.Positional,
            "Rotational" => CameraShakeType.Rotational,
            _ => throw new ArgumentException($"Unknown CameraShakeType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CameraShakeType result) {
        switch (value) {
            case "Positional":
                result = CameraShakeType.Positional;
                return true;
            case "Rotational":
                result = CameraShakeType.Rotational;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
