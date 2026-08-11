using System;

namespace BedrockProtocol.Enums;

public enum ControlScheme {
    locked_player_relative_strafe = 0,
    camera_relative = 1,
    camera_relative_strafe = 2,
    player_relative = 3,
    player_relative_strafe = 4,
}

public static class ControlSchemeExtensions {
    public static string ToProtoString(this ControlScheme value) => value.ToProtocolString();

    public static string ToProtocolString(this ControlScheme value) {
        return value switch {
            ControlScheme.locked_player_relative_strafe => "locked_player_relative_strafe",
            ControlScheme.camera_relative => "camera_relative",
            ControlScheme.camera_relative_strafe => "camera_relative_strafe",
            ControlScheme.player_relative => "player_relative",
            ControlScheme.player_relative_strafe => "player_relative_strafe",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ControlScheme value.")
        };
    }

    public static ControlScheme FromProtocolString(string value) {
        return value switch {
            "locked_player_relative_strafe" => ControlScheme.locked_player_relative_strafe,
            "camera_relative" => ControlScheme.camera_relative,
            "camera_relative_strafe" => ControlScheme.camera_relative_strafe,
            "player_relative" => ControlScheme.player_relative,
            "player_relative_strafe" => ControlScheme.player_relative_strafe,
            _ => throw new ArgumentException($"Unknown ControlScheme protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ControlScheme result) {
        switch (value) {
            case "locked_player_relative_strafe":
                result = ControlScheme.locked_player_relative_strafe;
                return true;
            case "camera_relative":
                result = ControlScheme.camera_relative;
                return true;
            case "camera_relative_strafe":
                result = ControlScheme.camera_relative_strafe;
                return true;
            case "player_relative":
                result = ControlScheme.player_relative;
                return true;
            case "player_relative_strafe":
                result = ControlScheme.player_relative_strafe;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
