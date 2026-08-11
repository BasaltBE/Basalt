using System;

namespace BedrockProtocol.Enums;

public enum CameraAimAssistPacketPayloadAction {
    Set = 0,
    Clear = 1,
}

public static class CameraAimAssistPacketPayloadActionExtensions {
    public static string ToProtoString(this CameraAimAssistPacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this CameraAimAssistPacketPayloadAction value) {
        return value switch {
            CameraAimAssistPacketPayloadAction.Set => "Set",
            CameraAimAssistPacketPayloadAction.Clear => "Clear",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CameraAimAssistPacketPayloadAction value.")
        };
    }

    public static CameraAimAssistPacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "Set" => CameraAimAssistPacketPayloadAction.Set,
            "Clear" => CameraAimAssistPacketPayloadAction.Clear,
            _ => throw new ArgumentException($"Unknown CameraAimAssistPacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CameraAimAssistPacketPayloadAction result) {
        switch (value) {
            case "Set":
                result = CameraAimAssistPacketPayloadAction.Set;
                return true;
            case "Clear":
                result = CameraAimAssistPacketPayloadAction.Clear;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
