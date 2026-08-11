using System;

namespace BedrockProtocol.Enums;

public enum SpawnBiomeType {
    Default = 0,
    UserDefined = 1,
}

public static class SpawnBiomeTypeExtensions {
    public static string ToProtoString(this SpawnBiomeType value) => value.ToProtocolString();

    public static string ToProtocolString(this SpawnBiomeType value) {
        return value switch {
            SpawnBiomeType.Default => "Default",
            SpawnBiomeType.UserDefined => "UserDefined",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SpawnBiomeType value.")
        };
    }

    public static SpawnBiomeType FromProtocolString(string value) {
        return value switch {
            "Default" => SpawnBiomeType.Default,
            "UserDefined" => SpawnBiomeType.UserDefined,
            _ => throw new ArgumentException($"Unknown SpawnBiomeType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SpawnBiomeType result) {
        switch (value) {
            case "Default":
                result = SpawnBiomeType.Default;
                return true;
            case "UserDefined":
                result = SpawnBiomeType.UserDefined;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
