#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ItemDescriptorType {
    Empty = 0,
    ItemName = 1,
    Molang = 2,
    ItemTag = 3,
}

public static class ItemDescriptorTypeExtensions {
    public static string ToProtoString(this ItemDescriptorType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemDescriptorType value) {
        return value switch {
            ItemDescriptorType.Empty => "Empty",
            ItemDescriptorType.ItemName => "ItemName",
            ItemDescriptorType.Molang => "Molang",
            ItemDescriptorType.ItemTag => "ItemTag",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemDescriptorType value.")
        };
    }

    public static ItemDescriptorType FromProtocolString(string value) {
        return value switch {
            "Empty" => ItemDescriptorType.Empty,
            "ItemName" => ItemDescriptorType.ItemName,
            "Molang" => ItemDescriptorType.Molang,
            "ItemTag" => ItemDescriptorType.ItemTag,
            _ => throw new ArgumentException($"Unknown ItemDescriptorType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemDescriptorType result) {
        switch (value) {
            case "Empty":
                result = ItemDescriptorType.Empty;
                return true;
            case "ItemName":
                result = ItemDescriptorType.ItemName;
                return true;
            case "Molang":
                result = ItemDescriptorType.Molang;
                return true;
            case "ItemTag":
                result = ItemDescriptorType.ItemTag;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
