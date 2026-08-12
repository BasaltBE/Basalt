#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum Mirror {
    None = 0,
    X = 1,
    Z = 2,
    XZ = 3,
}

public static class MirrorExtensions {
    public static string ToProtoString(this Mirror value) => value.ToProtocolString();

    public static string ToProtocolString(this Mirror value) {
        return value switch {
            Mirror.None => "None",
            Mirror.X => "X",
            Mirror.Z => "Z",
            Mirror.XZ => "XZ",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Mirror value.")
        };
    }

    public static Mirror FromProtocolString(string value) {
        return value switch {
            "None" => Mirror.None,
            "X" => Mirror.X,
            "Z" => Mirror.Z,
            "XZ" => Mirror.XZ,
            _ => throw new ArgumentException($"Unknown Mirror protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out Mirror result) {
        switch (value) {
            case "None":
                result = Mirror.None;
                return true;
            case "X":
                result = Mirror.X;
                return true;
            case "Z":
                result = Mirror.Z;
                return true;
            case "XZ":
                result = Mirror.XZ;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
