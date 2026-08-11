using System;

namespace BedrockProtocol.Enums;

public enum PositionMode {
    Normal = 0,
    Respawn = 1,
    Teleport = 2,
    OnlyHeadRot = 3,
}

public static class PositionModeExtensions {
    public static string ToProtoString(this PositionMode value) => value.ToProtocolString();

    public static string ToProtocolString(this PositionMode value) {
        return value switch {
            PositionMode.Normal => "Normal",
            PositionMode.Respawn => "Respawn",
            PositionMode.Teleport => "Teleport",
            PositionMode.OnlyHeadRot => "OnlyHeadRot",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PositionMode value.")
        };
    }

    public static PositionMode FromProtocolString(string value) {
        return value switch {
            "Normal" => PositionMode.Normal,
            "Respawn" => PositionMode.Respawn,
            "Teleport" => PositionMode.Teleport,
            "OnlyHeadRot" => PositionMode.OnlyHeadRot,
            _ => throw new ArgumentException($"Unknown PositionMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PositionMode result) {
        switch (value) {
            case "Normal":
                result = PositionMode.Normal;
                return true;
            case "Respawn":
                result = PositionMode.Respawn;
                return true;
            case "Teleport":
                result = PositionMode.Teleport;
                return true;
            case "OnlyHeadRot":
                result = PositionMode.OnlyHeadRot;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
