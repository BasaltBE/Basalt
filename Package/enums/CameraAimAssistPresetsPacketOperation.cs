using System;

namespace BedrockProtocol.Enums;

public enum CameraAimAssistPresetsPacketOperation {
    Set = 0,
    AddToExisting = 1,
}

public static class CameraAimAssistPresetsPacketOperationExtensions {
    public static string ToProtoString(this CameraAimAssistPresetsPacketOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this CameraAimAssistPresetsPacketOperation value) {
        return value switch {
            CameraAimAssistPresetsPacketOperation.Set => "Set",
            CameraAimAssistPresetsPacketOperation.AddToExisting => "AddToExisting",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CameraAimAssistPresetsPacketOperation value.")
        };
    }

    public static CameraAimAssistPresetsPacketOperation FromProtocolString(string value) {
        return value switch {
            "Set" => CameraAimAssistPresetsPacketOperation.Set,
            "AddToExisting" => CameraAimAssistPresetsPacketOperation.AddToExisting,
            _ => throw new ArgumentException($"Unknown CameraAimAssistPresetsPacketOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CameraAimAssistPresetsPacketOperation result) {
        switch (value) {
            case "Set":
                result = CameraAimAssistPresetsPacketOperation.Set;
                return true;
            case "AddToExisting":
                result = CameraAimAssistPresetsPacketOperation.AddToExisting;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
