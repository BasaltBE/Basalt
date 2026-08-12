#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ClientCameraAimAssistPacketAction {
    SetFromCameraPreset = 0,
    Clear = 1,
}

public static class ClientCameraAimAssistPacketActionExtensions {
    public static string ToProtoString(this ClientCameraAimAssistPacketAction value) => value.ToProtocolString();

    public static string ToProtocolString(this ClientCameraAimAssistPacketAction value) {
        return value switch {
            ClientCameraAimAssistPacketAction.SetFromCameraPreset => "SetFromCameraPreset",
            ClientCameraAimAssistPacketAction.Clear => "Clear",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ClientCameraAimAssistPacketAction value.")
        };
    }

    public static ClientCameraAimAssistPacketAction FromProtocolString(string value) {
        return value switch {
            "SetFromCameraPreset" => ClientCameraAimAssistPacketAction.SetFromCameraPreset,
            "Clear" => ClientCameraAimAssistPacketAction.Clear,
            _ => throw new ArgumentException($"Unknown ClientCameraAimAssistPacketAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ClientCameraAimAssistPacketAction result) {
        switch (value) {
            case "SetFromCameraPreset":
                result = ClientCameraAimAssistPacketAction.SetFromCameraPreset;
                return true;
            case "Clear":
                result = ClientCameraAimAssistPacketAction.Clear;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
