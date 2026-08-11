using System;

namespace BedrockProtocol.Enums;

public enum NpcDialogueActionType {
    Open = 0,
    Close = 1,
}

public static class NpcDialogueActionTypeExtensions {
    public static string ToProtoString(this NpcDialogueActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this NpcDialogueActionType value) {
        return value switch {
            NpcDialogueActionType.Open => "Open",
            NpcDialogueActionType.Close => "Close",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown NpcDialogueActionType value.")
        };
    }

    public static NpcDialogueActionType FromProtocolString(string value) {
        return value switch {
            "Open" => NpcDialogueActionType.Open,
            "Close" => NpcDialogueActionType.Close,
            _ => throw new ArgumentException($"Unknown NpcDialogueActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out NpcDialogueActionType result) {
        switch (value) {
            case "Open":
                result = NpcDialogueActionType.Open;
                return true;
            case "Close":
                result = NpcDialogueActionType.Close;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
