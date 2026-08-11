using System;

namespace BedrockProtocol.Enums;

public enum ItemUseActionType {
    Place = 0,
    Use = 1,
    Destroy = 2,
    UseAsAttack = 3,
}

public static class ItemUseActionTypeExtensions {
    public static string ToProtoString(this ItemUseActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemUseActionType value) {
        return value switch {
            ItemUseActionType.Place => "Place",
            ItemUseActionType.Use => "Use",
            ItemUseActionType.Destroy => "Destroy",
            ItemUseActionType.UseAsAttack => "Use As Attack",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemUseActionType value.")
        };
    }

    public static ItemUseActionType FromProtocolString(string value) {
        return value switch {
            "Place" => ItemUseActionType.Place,
            "Use" => ItemUseActionType.Use,
            "Destroy" => ItemUseActionType.Destroy,
            "Use As Attack" => ItemUseActionType.UseAsAttack,
            _ => throw new ArgumentException($"Unknown ItemUseActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemUseActionType result) {
        switch (value) {
            case "Place":
                result = ItemUseActionType.Place;
                return true;
            case "Use":
                result = ItemUseActionType.Use;
                return true;
            case "Destroy":
                result = ItemUseActionType.Destroy;
                return true;
            case "Use As Attack":
                result = ItemUseActionType.UseAsAttack;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
