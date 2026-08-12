#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum Rotation {
    None = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3,
    Clockwise90 = 1,
    Clockwise180 = 2,
    CounterClockwise90 = 3,
}

public static class RotationExtensions {
    public static string ToProtoString(this Rotation value) => value.ToProtocolString();

    public static string ToProtocolString(this Rotation value) {
        return value switch {
            Rotation.None => "None",
            Rotation.Rotate90 => "Rotate90",
            Rotation.Rotate180 => "Rotate180",
            Rotation.Rotate270 => "Rotate270",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Rotation value.")
        };
    }

    public static Rotation FromProtocolString(string value) {
        return value switch {
            "None" => Rotation.None,
            "Rotate90" => Rotation.Rotate90,
            "Rotate180" => Rotation.Rotate180,
            "Rotate270" => Rotation.Rotate270,
            "Clockwise90" => Rotation.Clockwise90,
            "Clockwise180" => Rotation.Clockwise180,
            "CounterClockwise90" => Rotation.CounterClockwise90,
            _ => throw new ArgumentException($"Unknown Rotation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out Rotation result) {
        switch (value) {
            case "None":
                result = Rotation.None;
                return true;
            case "Rotate90":
                result = Rotation.Rotate90;
                return true;
            case "Rotate180":
                result = Rotation.Rotate180;
                return true;
            case "Rotate270":
                result = Rotation.Rotate270;
                return true;
            case "Clockwise90":
                result = Rotation.Clockwise90;
                return true;
            case "Clockwise180":
                result = Rotation.Clockwise180;
                return true;
            case "CounterClockwise90":
                result = Rotation.CounterClockwise90;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
