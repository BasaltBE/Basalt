#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ScorePacketEntryAction {
    Remove = 0,
    ChangePlayer = 1,
    ChangeEntity = 2,
    ChangeFakePlayer = 3,
}

public static class ScorePacketEntryActionExtensions {
    public static string ToProtoString(this ScorePacketEntryAction value) => value.ToProtocolString();

    public static string ToProtocolString(this ScorePacketEntryAction value) {
        return value switch {
            ScorePacketEntryAction.Remove => "Remove",
            ScorePacketEntryAction.ChangePlayer => "ChangePlayer",
            ScorePacketEntryAction.ChangeEntity => "ChangeEntity",
            ScorePacketEntryAction.ChangeFakePlayer => "ChangeFakePlayer",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ScorePacketEntryAction value.")
        };
    }

    public static ScorePacketEntryAction FromProtocolString(string value) {
        return value switch {
            "Remove" => ScorePacketEntryAction.Remove,
            "ChangePlayer" => ScorePacketEntryAction.ChangePlayer,
            "ChangeEntity" => ScorePacketEntryAction.ChangeEntity,
            "ChangeFakePlayer" => ScorePacketEntryAction.ChangeFakePlayer,
            _ => throw new ArgumentException($"Unknown ScorePacketEntryAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ScorePacketEntryAction result) {
        switch (value) {
            case "Remove":
                result = ScorePacketEntryAction.Remove;
                return true;
            case "ChangePlayer":
                result = ScorePacketEntryAction.ChangePlayer;
                return true;
            case "ChangeEntity":
                result = ScorePacketEntryAction.ChangeEntity;
                return true;
            case "ChangeFakePlayer":
                result = ScorePacketEntryAction.ChangeFakePlayer;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
