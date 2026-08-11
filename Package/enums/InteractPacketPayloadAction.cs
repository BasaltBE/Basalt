using System;

namespace BedrockProtocol.Enums;

public enum InteractPacketPayloadAction {
    Invalid = 0,
    StopRiding = 3,
    InteractUpdate = 4,
    NpcOpen = 5,
    OpenInventory = 6,
}

public static class InteractPacketPayloadActionExtensions {
    public static string ToProtoString(this InteractPacketPayloadAction value) => value.ToProtocolString();

    public static string ToProtocolString(this InteractPacketPayloadAction value) {
        return value switch {
            InteractPacketPayloadAction.Invalid => "Invalid",
            InteractPacketPayloadAction.StopRiding => "StopRiding",
            InteractPacketPayloadAction.InteractUpdate => "InteractUpdate",
            InteractPacketPayloadAction.NpcOpen => "NpcOpen",
            InteractPacketPayloadAction.OpenInventory => "OpenInventory",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InteractPacketPayloadAction value.")
        };
    }

    public static InteractPacketPayloadAction FromProtocolString(string value) {
        return value switch {
            "Invalid" => InteractPacketPayloadAction.Invalid,
            "StopRiding" => InteractPacketPayloadAction.StopRiding,
            "InteractUpdate" => InteractPacketPayloadAction.InteractUpdate,
            "NpcOpen" => InteractPacketPayloadAction.NpcOpen,
            "OpenInventory" => InteractPacketPayloadAction.OpenInventory,
            _ => throw new ArgumentException($"Unknown InteractPacketPayloadAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InteractPacketPayloadAction result) {
        switch (value) {
            case "Invalid":
                result = InteractPacketPayloadAction.Invalid;
                return true;
            case "StopRiding":
                result = InteractPacketPayloadAction.StopRiding;
                return true;
            case "InteractUpdate":
                result = InteractPacketPayloadAction.InteractUpdate;
                return true;
            case "NpcOpen":
                result = InteractPacketPayloadAction.NpcOpen;
                return true;
            case "OpenInventory":
                result = InteractPacketPayloadAction.OpenInventory;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
