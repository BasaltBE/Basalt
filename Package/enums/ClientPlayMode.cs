using System;

namespace BedrockProtocol.Enums;

public enum ClientPlayMode {
    Normal = 0,
    Teaser = 1,
    Screen = 2,
    ExitLevel = 7,
    NumModes = 9,
}

public static class ClientPlayModeExtensions {
    public static string ToProtoString(this ClientPlayMode value) => value.ToProtocolString();

    public static string ToProtocolString(this ClientPlayMode value) {
        return value switch {
            ClientPlayMode.Normal => "Normal",
            ClientPlayMode.Teaser => "Teaser",
            ClientPlayMode.Screen => "Screen",
            ClientPlayMode.ExitLevel => "ExitLevel",
            ClientPlayMode.NumModes => "NumModes",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ClientPlayMode value.")
        };
    }

    public static ClientPlayMode FromProtocolString(string value) {
        return value switch {
            "Normal" => ClientPlayMode.Normal,
            "Teaser" => ClientPlayMode.Teaser,
            "Screen" => ClientPlayMode.Screen,
            "ExitLevel" => ClientPlayMode.ExitLevel,
            "NumModes" => ClientPlayMode.NumModes,
            _ => throw new ArgumentException($"Unknown ClientPlayMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ClientPlayMode result) {
        switch (value) {
            case "Normal":
                result = ClientPlayMode.Normal;
                return true;
            case "Teaser":
                result = ClientPlayMode.Teaser;
                return true;
            case "Screen":
                result = ClientPlayMode.Screen;
                return true;
            case "ExitLevel":
                result = ClientPlayMode.ExitLevel;
                return true;
            case "NumModes":
                result = ClientPlayMode.NumModes;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
