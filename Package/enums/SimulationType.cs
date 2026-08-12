#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum SimulationType {
    Game = 0,
    Editor = 1,
    Test = 2,
    INVALID = 3,
}

public static class SimulationTypeExtensions {
    public static string ToProtoString(this SimulationType value) => value.ToProtocolString();

    public static string ToProtocolString(this SimulationType value) {
        return value switch {
            SimulationType.Game => "Game",
            SimulationType.Editor => "Editor",
            SimulationType.Test => "Test",
            SimulationType.INVALID => "INVALID",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SimulationType value.")
        };
    }

    public static SimulationType FromProtocolString(string value) {
        return value switch {
            "Game" => SimulationType.Game,
            "Editor" => SimulationType.Editor,
            "Test" => SimulationType.Test,
            "INVALID" => SimulationType.INVALID,
            _ => throw new ArgumentException($"Unknown SimulationType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SimulationType result) {
        switch (value) {
            case "Game":
                result = SimulationType.Game;
                return true;
            case "Editor":
                result = SimulationType.Editor;
                return true;
            case "Test":
                result = SimulationType.Test;
                return true;
            case "INVALID":
                result = SimulationType.INVALID;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
