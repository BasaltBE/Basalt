using System;

namespace BedrockProtocol.Enums;

public enum RewindType {
    Player = 0,
    Vehicle = 1,
}

public static class RewindTypeExtensions {
    public static string ToProtoString(this RewindType value) => value.ToProtocolString();

    public static string ToProtocolString(this RewindType value) {
        return value switch {
            RewindType.Player => "Player",
            RewindType.Vehicle => "Vehicle",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown RewindType value.")
        };
    }

    public static RewindType FromProtocolString(string value) {
        return value switch {
            "Player" => RewindType.Player,
            "Vehicle" => RewindType.Vehicle,
            _ => throw new ArgumentException($"Unknown RewindType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out RewindType result) {
        switch (value) {
            case "Player":
                result = RewindType.Player;
                return true;
            case "Vehicle":
                result = RewindType.Vehicle;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
