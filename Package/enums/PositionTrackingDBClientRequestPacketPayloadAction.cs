using System;

namespace BedrockProtocol.Enums;

public enum PositionTrackingDBClientRequestPacketPayloadAction {
    Query = 0,
}

public static class PositionTrackingDBClientRequestPacketPayloadActionExtensions {
    public static string ToProtoString(this PositionTrackingDBClientRequestPacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this PositionTrackingDBClientRequestPacketPayloadAction value) {
        return value switch {
            PositionTrackingDBClientRequestPacketPayloadAction.Query => "Query",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PositionTrackingDBClientRequestPacketPayloadAction value.")
        };
    }

    public static PositionTrackingDBClientRequestPacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "Query" => PositionTrackingDBClientRequestPacketPayloadAction.Query,
            _ => throw new ArgumentException($"Unknown PositionTrackingDBClientRequestPacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PositionTrackingDBClientRequestPacketPayloadAction result) {
        switch (value) {
            case "Query":
                result = PositionTrackingDBClientRequestPacketPayloadAction.Query;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
