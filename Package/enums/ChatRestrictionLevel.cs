using System;

namespace BedrockProtocol.Enums;

public enum ChatRestrictionLevel {
    None = 0,
    Dropped = 1,
    Disabled = 2,
}

public static class ChatRestrictionLevelExtensions {
    public static string ToProtoString(this ChatRestrictionLevel value) => value.ToProtocolString();

    public static string ToProtocolString(this ChatRestrictionLevel value) {
        return value switch {
            ChatRestrictionLevel.None => "None",
            ChatRestrictionLevel.Dropped => "Dropped",
            ChatRestrictionLevel.Disabled => "Disabled",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ChatRestrictionLevel value.")
        };
    }

    public static ChatRestrictionLevel FromProtocolString(string value) {
        return value switch {
            "None" => ChatRestrictionLevel.None,
            "Dropped" => ChatRestrictionLevel.Dropped,
            "Disabled" => ChatRestrictionLevel.Disabled,
            _ => throw new ArgumentException($"Unknown ChatRestrictionLevel protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ChatRestrictionLevel result) {
        switch (value) {
            case "None":
                result = ChatRestrictionLevel.None;
                return true;
            case "Dropped":
                result = ChatRestrictionLevel.Dropped;
                return true;
            case "Disabled":
                result = ChatRestrictionLevel.Disabled;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
