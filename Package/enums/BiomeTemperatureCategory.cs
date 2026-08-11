using System;

namespace BedrockProtocol.Enums;

public enum BiomeTemperatureCategory {
    Medium = 0,
    Warm = 1,
    Lukewarm = 2,
    Cold = 3,
    Frozen = 4,
}

public static class BiomeTemperatureCategoryExtensions {
    public static string ToProtoString(this BiomeTemperatureCategory value) => value.ToProtocolString();

    public static string ToProtocolString(this BiomeTemperatureCategory value) {
        return value switch {
            BiomeTemperatureCategory.Medium => "Medium",
            BiomeTemperatureCategory.Warm => "Warm",
            BiomeTemperatureCategory.Lukewarm => "Lukewarm",
            BiomeTemperatureCategory.Cold => "Cold",
            BiomeTemperatureCategory.Frozen => "Frozen",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BiomeTemperatureCategory value.")
        };
    }

    public static BiomeTemperatureCategory FromProtocolString(string value) {
        return value switch {
            "Medium" => BiomeTemperatureCategory.Medium,
            "Warm" => BiomeTemperatureCategory.Warm,
            "Lukewarm" => BiomeTemperatureCategory.Lukewarm,
            "Cold" => BiomeTemperatureCategory.Cold,
            "Frozen" => BiomeTemperatureCategory.Frozen,
            _ => throw new ArgumentException($"Unknown BiomeTemperatureCategory protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BiomeTemperatureCategory result) {
        switch (value) {
            case "Medium":
                result = BiomeTemperatureCategory.Medium;
                return true;
            case "Warm":
                result = BiomeTemperatureCategory.Warm;
                return true;
            case "Lukewarm":
                result = BiomeTemperatureCategory.Lukewarm;
                return true;
            case "Cold":
                result = BiomeTemperatureCategory.Cold;
                return true;
            case "Frozen":
                result = BiomeTemperatureCategory.Frozen;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
