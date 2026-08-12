#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CameraAimAssistPacketPayloadTargetMode {
    Angle = 0,
    Distance = 1,
}

public static class CameraAimAssistPacketPayloadTargetModeExtensions {
    public static string ToProtoString(this CameraAimAssistPacketPayloadTargetMode value) => value.ToProtocolString();

    public static string ToProtocolString(this CameraAimAssistPacketPayloadTargetMode value) {
        return value switch {
            CameraAimAssistPacketPayloadTargetMode.Angle => "Angle",
            CameraAimAssistPacketPayloadTargetMode.Distance => "Distance",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CameraAimAssistPacketPayloadTargetMode value.")
        };
    }

    public static CameraAimAssistPacketPayloadTargetMode FromProtocolString(string value) {
        return value switch {
            "Angle" => CameraAimAssistPacketPayloadTargetMode.Angle,
            "Distance" => CameraAimAssistPacketPayloadTargetMode.Distance,
            _ => throw new ArgumentException($"Unknown CameraAimAssistPacketPayloadTargetMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CameraAimAssistPacketPayloadTargetMode result) {
        switch (value) {
            case "Angle":
                result = CameraAimAssistPacketPayloadTargetMode.Angle;
                return true;
            case "Distance":
                result = CameraAimAssistPacketPayloadTargetMode.Distance;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
