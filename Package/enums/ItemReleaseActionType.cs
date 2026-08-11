using System;

namespace BedrockProtocol.Enums;

public enum ItemReleaseActionType {
    Release = 0,
    Use = 1,
}

public static class ItemReleaseActionTypeExtensions {
    public static string ToProtoString(this ItemReleaseActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemReleaseActionType value) {
        return value switch {
            ItemReleaseActionType.Release => "Release",
            ItemReleaseActionType.Use => "Use",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemReleaseActionType value.")
        };
    }

    public static ItemReleaseActionType FromProtocolString(string value) {
        return value switch {
            "Release" => ItemReleaseActionType.Release,
            "Use" => ItemReleaseActionType.Use,
            _ => throw new ArgumentException($"Unknown ItemReleaseActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemReleaseActionType result) {
        switch (value) {
            case "Release":
                result = ItemReleaseActionType.Release;
                return true;
            case "Use":
                result = ItemReleaseActionType.Use;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
