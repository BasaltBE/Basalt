#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum GameType {
    Undefined = -1,
    Survival = 0,
    Creative = 1,
    Adventure = 2,
    Default = 5,
    Spectator = 6,
    WorldDefault = 0,
}

public static class GameTypeExtensions {
    public static string ToProtoString(this GameType value) => value.ToProtocolString();

    public static string ToProtocolString(this GameType value) {
        return value switch {
            GameType.Undefined => "Undefined",
            GameType.Survival => "Survival",
            GameType.Creative => "Creative",
            GameType.Adventure => "Adventure",
            GameType.Default => "Default",
            GameType.Spectator => "Spectator",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown GameType value.")
        };
    }

    public static GameType FromProtocolString(string value) {
        return value switch {
            "Undefined" => GameType.Undefined,
            "Survival" => GameType.Survival,
            "Creative" => GameType.Creative,
            "Adventure" => GameType.Adventure,
            "Default" => GameType.Default,
            "Spectator" => GameType.Spectator,
            "WorldDefault" => GameType.WorldDefault,
            _ => throw new ArgumentException($"Unknown GameType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out GameType result) {
        switch (value) {
            case "Undefined":
                result = GameType.Undefined;
                return true;
            case "Survival":
                result = GameType.Survival;
                return true;
            case "Creative":
                result = GameType.Creative;
                return true;
            case "Adventure":
                result = GameType.Adventure;
                return true;
            case "Default":
                result = GameType.Default;
                return true;
            case "Spectator":
                result = GameType.Spectator;
                return true;
            case "WorldDefault":
                result = GameType.WorldDefault;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
