using System;

namespace BedrockProtocol.Enums;

public enum ContainerID {
    CONTAINER_ID_NONE = -1,
    CONTAINER_ID_INVENTORY = 0,
    CONTAINER_ID_FIRST = 1,
    CONTAINER_ID_LAST = 100,
    CONTAINER_ID_OFFHAND = 119,
    CONTAINER_ID_ARMOR = 120,
    CONTAINER_ID_SELECTION_SLOTS = 122,
    CONTAINER_ID_PLAYER_ONLY_UI = 124,
    CONTAINER_ID_REGISTRY = 125,
}

public static class ContainerIDExtensions {
    public static string ToProtoString(this ContainerID value) => value.ToProtocolString();

    public static string ToProtocolString(this ContainerID value) {
        return value switch {
            ContainerID.CONTAINER_ID_NONE => "CONTAINER_ID_NONE",
            ContainerID.CONTAINER_ID_INVENTORY => "CONTAINER_ID_INVENTORY",
            ContainerID.CONTAINER_ID_FIRST => "CONTAINER_ID_FIRST",
            ContainerID.CONTAINER_ID_LAST => "CONTAINER_ID_LAST",
            ContainerID.CONTAINER_ID_OFFHAND => "CONTAINER_ID_OFFHAND",
            ContainerID.CONTAINER_ID_ARMOR => "CONTAINER_ID_ARMOR",
            ContainerID.CONTAINER_ID_SELECTION_SLOTS => "CONTAINER_ID_SELECTION_SLOTS",
            ContainerID.CONTAINER_ID_PLAYER_ONLY_UI => "CONTAINER_ID_PLAYER_ONLY_UI",
            ContainerID.CONTAINER_ID_REGISTRY => "CONTAINER_ID_REGISTRY",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ContainerID value.")
        };
    }

    public static ContainerID FromProtocolString(string value) {
        return value switch {
            "CONTAINER_ID_NONE" => ContainerID.CONTAINER_ID_NONE,
            "CONTAINER_ID_INVENTORY" => ContainerID.CONTAINER_ID_INVENTORY,
            "CONTAINER_ID_FIRST" => ContainerID.CONTAINER_ID_FIRST,
            "CONTAINER_ID_LAST" => ContainerID.CONTAINER_ID_LAST,
            "CONTAINER_ID_OFFHAND" => ContainerID.CONTAINER_ID_OFFHAND,
            "CONTAINER_ID_ARMOR" => ContainerID.CONTAINER_ID_ARMOR,
            "CONTAINER_ID_SELECTION_SLOTS" => ContainerID.CONTAINER_ID_SELECTION_SLOTS,
            "CONTAINER_ID_PLAYER_ONLY_UI" => ContainerID.CONTAINER_ID_PLAYER_ONLY_UI,
            "CONTAINER_ID_REGISTRY" => ContainerID.CONTAINER_ID_REGISTRY,
            _ => throw new ArgumentException($"Unknown ContainerID protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ContainerID result) {
        switch (value) {
            case "CONTAINER_ID_NONE":
                result = ContainerID.CONTAINER_ID_NONE;
                return true;
            case "CONTAINER_ID_INVENTORY":
                result = ContainerID.CONTAINER_ID_INVENTORY;
                return true;
            case "CONTAINER_ID_FIRST":
                result = ContainerID.CONTAINER_ID_FIRST;
                return true;
            case "CONTAINER_ID_LAST":
                result = ContainerID.CONTAINER_ID_LAST;
                return true;
            case "CONTAINER_ID_OFFHAND":
                result = ContainerID.CONTAINER_ID_OFFHAND;
                return true;
            case "CONTAINER_ID_ARMOR":
                result = ContainerID.CONTAINER_ID_ARMOR;
                return true;
            case "CONTAINER_ID_SELECTION_SLOTS":
                result = ContainerID.CONTAINER_ID_SELECTION_SLOTS;
                return true;
            case "CONTAINER_ID_PLAYER_ONLY_UI":
                result = ContainerID.CONTAINER_ID_PLAYER_ONLY_UI;
                return true;
            case "CONTAINER_ID_REGISTRY":
                result = ContainerID.CONTAINER_ID_REGISTRY;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
