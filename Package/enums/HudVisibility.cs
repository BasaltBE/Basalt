#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum HudVisibility {
    Hide = 0,
    Reset = 1,
}

public static class HudVisibilityExtensions {
    public static string ToProtoString(this HudVisibility value) => value.ToProtocolString();

    public static string ToProtocolString(this HudVisibility value) {
        return value switch {
            HudVisibility.Hide => "Hide",
            HudVisibility.Reset => "Reset",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown HudVisibility value.")
        };
    }

    public static HudVisibility FromProtocolString(string value) {
        return value switch {
            "Hide" => HudVisibility.Hide,
            "Reset" => HudVisibility.Reset,
            _ => throw new ArgumentException($"Unknown HudVisibility protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out HudVisibility result) {
        switch (value) {
            case "Hide":
                result = HudVisibility.Hide;
                return true;
            case "Reset":
                result = HudVisibility.Reset;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
