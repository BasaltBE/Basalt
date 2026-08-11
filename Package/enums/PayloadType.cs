using System;

namespace BedrockProtocol.Enums;

public enum PayloadType {
    Invalid = 0,
    ClearDebugMarkers = 1,
    AddDebugMarkerCube = 2,
}

public static class PayloadTypeExtensions {
    public static string ToProtoString(this PayloadType value) => value.ToProtocolString();

    public static string ToProtocolString(this PayloadType value) {
        return value switch {
            PayloadType.Invalid => "Invalid",
            PayloadType.ClearDebugMarkers => "ClearDebugMarkers",
            PayloadType.AddDebugMarkerCube => "AddDebugMarkerCube",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PayloadType value.")
        };
    }

    public static PayloadType FromProtocolString(string value) {
        return value switch {
            "Invalid" => PayloadType.Invalid,
            "ClearDebugMarkers" => PayloadType.ClearDebugMarkers,
            "AddDebugMarkerCube" => PayloadType.AddDebugMarkerCube,
            _ => throw new ArgumentException($"Unknown PayloadType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PayloadType result) {
        switch (value) {
            case "Invalid":
                result = PayloadType.Invalid;
                return true;
            case "ClearDebugMarkers":
                result = PayloadType.ClearDebugMarkers;
                return true;
            case "AddDebugMarkerCube":
                result = PayloadType.AddDebugMarkerCube;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
