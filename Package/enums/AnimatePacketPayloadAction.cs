using System;

namespace BedrockProtocol.Enums;

public enum AnimatePacketPayloadAction {
    NoAction = 0,
    Swing = 1,
    WakeUp = 3,
    CriticalHit = 4,
    MagicCriticalHit = 5,
}

public static class AnimatePacketPayloadActionExtensions {
    public static string ToProtoString(this AnimatePacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this AnimatePacketPayloadAction value) {
        return value switch {
            AnimatePacketPayloadAction.NoAction => "NoAction",
            AnimatePacketPayloadAction.Swing => "Swing",
            AnimatePacketPayloadAction.WakeUp => "WakeUp",
            AnimatePacketPayloadAction.CriticalHit => "CriticalHit",
            AnimatePacketPayloadAction.MagicCriticalHit => "MagicCriticalHit",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AnimatePacketPayloadAction value.")
        };
    }

    public static AnimatePacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "NoAction" => AnimatePacketPayloadAction.NoAction,
            "Swing" => AnimatePacketPayloadAction.Swing,
            "WakeUp" => AnimatePacketPayloadAction.WakeUp,
            "CriticalHit" => AnimatePacketPayloadAction.CriticalHit,
            "MagicCriticalHit" => AnimatePacketPayloadAction.MagicCriticalHit,
            _ => throw new ArgumentException($"Unknown AnimatePacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AnimatePacketPayloadAction result) {
        switch (value) {
            case "NoAction":
                result = AnimatePacketPayloadAction.NoAction;
                return true;
            case "Swing":
                result = AnimatePacketPayloadAction.Swing;
                return true;
            case "WakeUp":
                result = AnimatePacketPayloadAction.WakeUp;
                return true;
            case "CriticalHit":
                result = AnimatePacketPayloadAction.CriticalHit;
                return true;
            case "MagicCriticalHit":
                result = AnimatePacketPayloadAction.MagicCriticalHit;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
