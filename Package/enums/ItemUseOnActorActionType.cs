using System;

namespace BedrockProtocol.Enums;

public enum ItemUseOnActorActionType {
    Interact = 0,
    Attack = 1,
    ItemInteract = 2,
}

public static class ItemUseOnActorActionTypeExtensions {
    public static string ToProtoString(this ItemUseOnActorActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemUseOnActorActionType value) {
        return value switch {
            ItemUseOnActorActionType.Interact => "Interact",
            ItemUseOnActorActionType.Attack => "Attack",
            ItemUseOnActorActionType.ItemInteract => "Item Interact",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemUseOnActorActionType value.")
        };
    }

    public static ItemUseOnActorActionType FromProtocolString(string value) {
        return value switch {
            "Interact" => ItemUseOnActorActionType.Interact,
            "Attack" => ItemUseOnActorActionType.Attack,
            "Item Interact" => ItemUseOnActorActionType.ItemInteract,
            _ => throw new ArgumentException($"Unknown ItemUseOnActorActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemUseOnActorActionType result) {
        switch (value) {
            case "Interact":
                result = ItemUseOnActorActionType.Interact;
                return true;
            case "Attack":
                result = ItemUseOnActorActionType.Attack;
                return true;
            case "Item Interact":
                result = ItemUseOnActorActionType.ItemInteract;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
