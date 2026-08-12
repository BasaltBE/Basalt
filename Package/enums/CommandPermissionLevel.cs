#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CommandPermissionLevel {
    Any = 0,
    GameDirectors = 1,
    Admin = 2,
    Host = 3,
    Owner = 4,
    Internal = 5,
}

public static class CommandPermissionLevelExtensions {
    public static string ToProtoString(this CommandPermissionLevel value) => value.ToProtocolString();

    public static string ToProtocolString(this CommandPermissionLevel value) {
        return value switch {
            CommandPermissionLevel.Any => "Any",
            CommandPermissionLevel.GameDirectors => "GameDirectors",
            CommandPermissionLevel.Admin => "Admin",
            CommandPermissionLevel.Host => "Host",
            CommandPermissionLevel.Owner => "Owner",
            CommandPermissionLevel.Internal => "Internal",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CommandPermissionLevel value.")
        };
    }

    public static CommandPermissionLevel FromProtocolString(string value) {
        return value switch {
            "Any" => CommandPermissionLevel.Any,
            "GameDirectors" => CommandPermissionLevel.GameDirectors,
            "Admin" => CommandPermissionLevel.Admin,
            "Host" => CommandPermissionLevel.Host,
            "Owner" => CommandPermissionLevel.Owner,
            "Internal" => CommandPermissionLevel.Internal,
            _ => throw new ArgumentException($"Unknown CommandPermissionLevel protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CommandPermissionLevel result) {
        switch (value) {
            case "Any":
                result = CommandPermissionLevel.Any;
                return true;
            case "GameDirectors":
                result = CommandPermissionLevel.GameDirectors;
                return true;
            case "Admin":
                result = CommandPermissionLevel.Admin;
                return true;
            case "Host":
                result = CommandPermissionLevel.Host;
                return true;
            case "Owner":
                result = CommandPermissionLevel.Owner;
                return true;
            case "Internal":
                result = CommandPermissionLevel.Internal;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
