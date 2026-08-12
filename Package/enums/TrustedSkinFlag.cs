#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum TrustedSkinFlag {
    Unset = 0,
    False = 1,
    True = 2,
}

public static class TrustedSkinFlagExtensions {
    public static string ToProtoString(this TrustedSkinFlag value) => value.ToProtocolString();

    public static string ToProtocolString(this TrustedSkinFlag value) {
        return value switch {
            TrustedSkinFlag.Unset => "Unset",
            TrustedSkinFlag.False => "False",
            TrustedSkinFlag.True => "True",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown TrustedSkinFlag value.")
        };
    }

    public static TrustedSkinFlag FromProtocolString(string value) {
        return value switch {
            "Unset" => TrustedSkinFlag.Unset,
            "False" => TrustedSkinFlag.False,
            "True" => TrustedSkinFlag.True,
            _ => throw new ArgumentException($"Unknown TrustedSkinFlag protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out TrustedSkinFlag result) {
        switch (value) {
            case "Unset":
                result = TrustedSkinFlag.Unset;
                return true;
            case "False":
                result = TrustedSkinFlag.False;
                return true;
            case "True":
                result = TrustedSkinFlag.True;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
