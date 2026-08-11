using System;

namespace BedrockProtocol.Enums;

public enum Difficulty {
    Peaceful = 0,
    Easy = 1,
    Normal = 2,
    Hard = 3,
    Count = 4,
    Unknown = 5,
}

public static class DifficultyExtensions {
    public static string ToProtoString(this Difficulty value) => value.ToProtocolString();

    public static string ToProtocolString(this Difficulty value) {
        return value switch {
            Difficulty.Peaceful => "Peaceful",
            Difficulty.Easy => "Easy",
            Difficulty.Normal => "Normal",
            Difficulty.Hard => "Hard",
            Difficulty.Count => "Count",
            Difficulty.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Difficulty value.")
        };
    }

    public static Difficulty FromProtocolString(string value) {
        return value switch {
            "Peaceful" => Difficulty.Peaceful,
            "Easy" => Difficulty.Easy,
            "Normal" => Difficulty.Normal,
            "Hard" => Difficulty.Hard,
            "Count" => Difficulty.Count,
            "Unknown" => Difficulty.Unknown,
            _ => throw new ArgumentException($"Unknown Difficulty protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out Difficulty result) {
        switch (value) {
            case "Peaceful":
                result = Difficulty.Peaceful;
                return true;
            case "Easy":
                result = Difficulty.Easy;
                return true;
            case "Normal":
                result = Difficulty.Normal;
                return true;
            case "Hard":
                result = Difficulty.Hard;
                return true;
            case "Count":
                result = Difficulty.Count;
                return true;
            case "Unknown":
                result = Difficulty.Unknown;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
