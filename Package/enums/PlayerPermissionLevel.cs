using System;

namespace BedrockProtocol.Enums;

public enum PlayerPermissionLevel {
    Visitor = 0,
    Member = 1,
    Operator = 2,
    Custom = 3,
}

public static class PlayerPermissionLevelExtensions {
    public static string ToProtoString(this PlayerPermissionLevel value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerPermissionLevel value) {
        return value switch {
            PlayerPermissionLevel.Visitor => "Visitor",
            PlayerPermissionLevel.Member => "Member",
            PlayerPermissionLevel.Operator => "Operator",
            PlayerPermissionLevel.Custom => "Custom",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerPermissionLevel value.")
        };
    }

    public static PlayerPermissionLevel FromProtocolString(string value) {
        return value switch {
            "Visitor" => PlayerPermissionLevel.Visitor,
            "Member" => PlayerPermissionLevel.Member,
            "Operator" => PlayerPermissionLevel.Operator,
            "Custom" => PlayerPermissionLevel.Custom,
            _ => throw new ArgumentException($"Unknown PlayerPermissionLevel protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerPermissionLevel result) {
        switch (value) {
            case "Visitor":
                result = PlayerPermissionLevel.Visitor;
                return true;
            case "Member":
                result = PlayerPermissionLevel.Member;
                return true;
            case "Operator":
                result = PlayerPermissionLevel.Operator;
                return true;
            case "Custom":
                result = PlayerPermissionLevel.Custom;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
