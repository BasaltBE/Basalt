using System;

namespace BedrockProtocol.Enums;

public enum MessageId {
    NONE = 0,
    CREATE = 1,
    DESTROY = 2,
}

public static class MessageIdExtensions {
    public static string ToProtoString(this MessageId value) => value.ToProtocolString();

    public static string ToProtocolString(this MessageId value) {
        return value switch {
            MessageId.NONE => "NONE",
            MessageId.CREATE => "CREATE",
            MessageId.DESTROY => "DESTROY",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MessageId value.")
        };
    }

    public static MessageId FromProtocolString(string value) {
        return value switch {
            "NONE" => MessageId.NONE,
            "CREATE" => MessageId.CREATE,
            "DESTROY" => MessageId.DESTROY,
            _ => throw new ArgumentException($"Unknown MessageId protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MessageId result) {
        switch (value) {
            case "NONE":
                result = MessageId.NONE;
                return true;
            case "CREATE":
                result = MessageId.CREATE;
                return true;
            case "DESTROY":
                result = MessageId.DESTROY;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
