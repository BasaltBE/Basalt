using System;

namespace BedrockProtocol.Enums;

public enum ScoreboardIdentityPacketType {
    Update = 0,
    Remove = 1,
}

public static class ScoreboardIdentityPacketTypeExtensions {
    public static string ToProtoString(this ScoreboardIdentityPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this ScoreboardIdentityPacketType value) {
        return value switch {
            ScoreboardIdentityPacketType.Update => "Update",
            ScoreboardIdentityPacketType.Remove => "Remove",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ScoreboardIdentityPacketType value.")
        };
    }

    public static ScoreboardIdentityPacketType FromProtocolString(string value) {
        return value switch {
            "Update" => ScoreboardIdentityPacketType.Update,
            "Remove" => ScoreboardIdentityPacketType.Remove,
            _ => throw new ArgumentException($"Unknown ScoreboardIdentityPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ScoreboardIdentityPacketType result) {
        switch (value) {
            case "Update":
                result = ScoreboardIdentityPacketType.Update;
                return true;
            case "Remove":
                result = ScoreboardIdentityPacketType.Remove;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
