#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum NpcRequestPacketPayloadRequestType {
    SetActions = 0,
    ExecuteAction = 1,
    ExecuteClosingCommands = 2,
    SetName = 3,
    SetSkin = 4,
    SetInteractText = 5,
    ExecuteOpeningCommands = 6,
}

public static class NpcRequestPacketPayloadRequestTypeExtensions {
    public static string ToProtoString(this NpcRequestPacketPayloadRequestType value) => value.ToProtocolString();

    public static string ToProtocolString(this NpcRequestPacketPayloadRequestType value) {
        return value switch {
            NpcRequestPacketPayloadRequestType.SetActions => "SetActions",
            NpcRequestPacketPayloadRequestType.ExecuteAction => "ExecuteAction",
            NpcRequestPacketPayloadRequestType.ExecuteClosingCommands => "ExecuteClosingCommands",
            NpcRequestPacketPayloadRequestType.SetName => "SetName",
            NpcRequestPacketPayloadRequestType.SetSkin => "SetSkin",
            NpcRequestPacketPayloadRequestType.SetInteractText => "SetInteractText",
            NpcRequestPacketPayloadRequestType.ExecuteOpeningCommands => "ExecuteOpeningCommands",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown NpcRequestPacketPayloadRequestType value.")
        };
    }

    public static NpcRequestPacketPayloadRequestType FromProtocolString(string value) {
        return value switch {
            "SetActions" => NpcRequestPacketPayloadRequestType.SetActions,
            "ExecuteAction" => NpcRequestPacketPayloadRequestType.ExecuteAction,
            "ExecuteClosingCommands" => NpcRequestPacketPayloadRequestType.ExecuteClosingCommands,
            "SetName" => NpcRequestPacketPayloadRequestType.SetName,
            "SetSkin" => NpcRequestPacketPayloadRequestType.SetSkin,
            "SetInteractText" => NpcRequestPacketPayloadRequestType.SetInteractText,
            "ExecuteOpeningCommands" => NpcRequestPacketPayloadRequestType.ExecuteOpeningCommands,
            _ => throw new ArgumentException($"Unknown NpcRequestPacketPayloadRequestType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out NpcRequestPacketPayloadRequestType result) {
        switch (value) {
            case "SetActions":
                result = NpcRequestPacketPayloadRequestType.SetActions;
                return true;
            case "ExecuteAction":
                result = NpcRequestPacketPayloadRequestType.ExecuteAction;
                return true;
            case "ExecuteClosingCommands":
                result = NpcRequestPacketPayloadRequestType.ExecuteClosingCommands;
                return true;
            case "SetName":
                result = NpcRequestPacketPayloadRequestType.SetName;
                return true;
            case "SetSkin":
                result = NpcRequestPacketPayloadRequestType.SetSkin;
                return true;
            case "SetInteractText":
                result = NpcRequestPacketPayloadRequestType.SetInteractText;
                return true;
            case "ExecuteOpeningCommands":
                result = NpcRequestPacketPayloadRequestType.ExecuteOpeningCommands;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
