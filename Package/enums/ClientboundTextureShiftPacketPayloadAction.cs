using System;

namespace BedrockProtocol.Enums;

public enum ClientboundTextureShiftPacketPayloadAction {
    Invalid = 0,
    Initialize = 1,
    Start = 2,
    SetEnabled = 3,
    Sync = 4,
}

public static class ClientboundTextureShiftPacketPayloadActionExtensions {
    public static string ToProtoString(this ClientboundTextureShiftPacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this ClientboundTextureShiftPacketPayloadAction value) {
        return value switch {
            ClientboundTextureShiftPacketPayloadAction.Invalid => "Invalid",
            ClientboundTextureShiftPacketPayloadAction.Initialize => "Initialize",
            ClientboundTextureShiftPacketPayloadAction.Start => "Start",
            ClientboundTextureShiftPacketPayloadAction.SetEnabled => "SetEnabled",
            ClientboundTextureShiftPacketPayloadAction.Sync => "Sync",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ClientboundTextureShiftPacketPayloadAction value.")
        };
    }

    public static ClientboundTextureShiftPacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "Invalid" => ClientboundTextureShiftPacketPayloadAction.Invalid,
            "Initialize" => ClientboundTextureShiftPacketPayloadAction.Initialize,
            "Start" => ClientboundTextureShiftPacketPayloadAction.Start,
            "SetEnabled" => ClientboundTextureShiftPacketPayloadAction.SetEnabled,
            "Sync" => ClientboundTextureShiftPacketPayloadAction.Sync,
            _ => throw new ArgumentException($"Unknown ClientboundTextureShiftPacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ClientboundTextureShiftPacketPayloadAction result) {
        switch (value) {
            case "Invalid":
                result = ClientboundTextureShiftPacketPayloadAction.Invalid;
                return true;
            case "Initialize":
                result = ClientboundTextureShiftPacketPayloadAction.Initialize;
                return true;
            case "Start":
                result = ClientboundTextureShiftPacketPayloadAction.Start;
                return true;
            case "SetEnabled":
                result = ClientboundTextureShiftPacketPayloadAction.SetEnabled;
                return true;
            case "Sync":
                result = ClientboundTextureShiftPacketPayloadAction.Sync;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
