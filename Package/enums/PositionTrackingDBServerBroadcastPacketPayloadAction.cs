using System;

namespace BedrockProtocol.Enums;

public enum PositionTrackingDBServerBroadcastPacketPayloadAction {
    Update = 0,
    Destroy = 1,
    NotFound = 2,
}

public static class PositionTrackingDBServerBroadcastPacketPayloadActionExtensions {
    public static string ToProtoString(this PositionTrackingDBServerBroadcastPacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this PositionTrackingDBServerBroadcastPacketPayloadAction value) {
        return value switch {
            PositionTrackingDBServerBroadcastPacketPayloadAction.Update => "Update",
            PositionTrackingDBServerBroadcastPacketPayloadAction.Destroy => "Destroy",
            PositionTrackingDBServerBroadcastPacketPayloadAction.NotFound => "NotFound",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PositionTrackingDBServerBroadcastPacketPayloadAction value.")
        };
    }

    public static PositionTrackingDBServerBroadcastPacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "Update" => PositionTrackingDBServerBroadcastPacketPayloadAction.Update,
            "Destroy" => PositionTrackingDBServerBroadcastPacketPayloadAction.Destroy,
            "NotFound" => PositionTrackingDBServerBroadcastPacketPayloadAction.NotFound,
            _ => throw new ArgumentException($"Unknown PositionTrackingDBServerBroadcastPacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PositionTrackingDBServerBroadcastPacketPayloadAction result) {
        switch (value) {
            case "Update":
                result = PositionTrackingDBServerBroadcastPacketPayloadAction.Update;
                return true;
            case "Destroy":
                result = PositionTrackingDBServerBroadcastPacketPayloadAction.Destroy;
                return true;
            case "NotFound":
                result = PositionTrackingDBServerBroadcastPacketPayloadAction.NotFound;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
