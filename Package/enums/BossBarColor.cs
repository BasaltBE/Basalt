using System;

namespace BedrockProtocol.Enums;

public enum BossBarColor {
    PINK = 0,
    BLUE = 1,
    RED = 2,
    GREEN = 3,
    YELLOW = 4,
    PURPLE = 5,
    REBECCA_PURPLE = 6,
    WHITE = 7,
}

public static class BossBarColorExtensions {
    public static string ToProtoString(this BossBarColor value) => value.ToProtocolString();

    public static string ToProtocolString(this BossBarColor value) {
        return value switch {
            BossBarColor.PINK => "PINK",
            BossBarColor.BLUE => "BLUE",
            BossBarColor.RED => "RED",
            BossBarColor.GREEN => "GREEN",
            BossBarColor.YELLOW => "YELLOW",
            BossBarColor.PURPLE => "PURPLE",
            BossBarColor.REBECCA_PURPLE => "REBECCA_PURPLE",
            BossBarColor.WHITE => "WHITE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BossBarColor value.")
        };
    }

    public static BossBarColor FromProtocolString(string value) {
        return value switch {
            "PINK" => BossBarColor.PINK,
            "BLUE" => BossBarColor.BLUE,
            "RED" => BossBarColor.RED,
            "GREEN" => BossBarColor.GREEN,
            "YELLOW" => BossBarColor.YELLOW,
            "PURPLE" => BossBarColor.PURPLE,
            "REBECCA_PURPLE" => BossBarColor.REBECCA_PURPLE,
            "WHITE" => BossBarColor.WHITE,
            _ => throw new ArgumentException($"Unknown BossBarColor protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BossBarColor result) {
        switch (value) {
            case "PINK":
                result = BossBarColor.PINK;
                return true;
            case "BLUE":
                result = BossBarColor.BLUE;
                return true;
            case "RED":
                result = BossBarColor.RED;
                return true;
            case "GREEN":
                result = BossBarColor.GREEN;
                return true;
            case "YELLOW":
                result = BossBarColor.YELLOW;
                return true;
            case "PURPLE":
                result = BossBarColor.PURPLE;
                return true;
            case "REBECCA_PURPLE":
                result = BossBarColor.REBECCA_PURPLE;
                return true;
            case "WHITE":
                result = BossBarColor.WHITE;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
