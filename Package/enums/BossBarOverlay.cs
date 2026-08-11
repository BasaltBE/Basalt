using System;

namespace BedrockProtocol.Enums;

public enum BossBarOverlay {
    PROGRESS = 0,
    NOTCHED_6 = 1,
    NOTCHED_10 = 2,
    NOTCHED_12 = 3,
    NOTCHED_20 = 4,
}

public static class BossBarOverlayExtensions {
    public static string ToProtoString(this BossBarOverlay value) => value.ToProtocolString();

    public static string ToProtocolString(this BossBarOverlay value) {
        return value switch {
            BossBarOverlay.PROGRESS => "PROGRESS",
            BossBarOverlay.NOTCHED_6 => "NOTCHED_6",
            BossBarOverlay.NOTCHED_10 => "NOTCHED_10",
            BossBarOverlay.NOTCHED_12 => "NOTCHED_12",
            BossBarOverlay.NOTCHED_20 => "NOTCHED_20",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BossBarOverlay value.")
        };
    }

    public static BossBarOverlay FromProtocolString(string value) {
        return value switch {
            "PROGRESS" => BossBarOverlay.PROGRESS,
            "NOTCHED_6" => BossBarOverlay.NOTCHED_6,
            "NOTCHED_10" => BossBarOverlay.NOTCHED_10,
            "NOTCHED_12" => BossBarOverlay.NOTCHED_12,
            "NOTCHED_20" => BossBarOverlay.NOTCHED_20,
            _ => throw new ArgumentException($"Unknown BossBarOverlay protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BossBarOverlay result) {
        switch (value) {
            case "PROGRESS":
                result = BossBarOverlay.PROGRESS;
                return true;
            case "NOTCHED_6":
                result = BossBarOverlay.NOTCHED_6;
                return true;
            case "NOTCHED_10":
                result = BossBarOverlay.NOTCHED_10;
                return true;
            case "NOTCHED_12":
                result = BossBarOverlay.NOTCHED_12;
                return true;
            case "NOTCHED_20":
                result = BossBarOverlay.NOTCHED_20;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
