using System;

namespace BedrockProtocol.Enums;

public enum CreativeItemCategory {
    All = 0,
    Construction = 1,
    Nature = 2,
    Equipment = 3,
    Items = 4,
    ItemCommandOnly = 5,
    Undefined = 6,
}

public static class CreativeItemCategoryExtensions {
    public static string ToProtoString(this CreativeItemCategory value) => value.ToProtocolString();

    public static string ToProtocolString(this CreativeItemCategory value) {
        return value switch {
            CreativeItemCategory.All => "All",
            CreativeItemCategory.Construction => "Construction",
            CreativeItemCategory.Nature => "Nature",
            CreativeItemCategory.Equipment => "Equipment",
            CreativeItemCategory.Items => "Items",
            CreativeItemCategory.ItemCommandOnly => "ItemCommandOnly",
            CreativeItemCategory.Undefined => "Undefined",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CreativeItemCategory value.")
        };
    }

    public static CreativeItemCategory FromProtocolString(string value) {
        return value switch {
            "All" => CreativeItemCategory.All,
            "Construction" => CreativeItemCategory.Construction,
            "Nature" => CreativeItemCategory.Nature,
            "Equipment" => CreativeItemCategory.Equipment,
            "Items" => CreativeItemCategory.Items,
            "ItemCommandOnly" => CreativeItemCategory.ItemCommandOnly,
            "Undefined" => CreativeItemCategory.Undefined,
            _ => throw new ArgumentException($"Unknown CreativeItemCategory protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CreativeItemCategory result) {
        switch (value) {
            case "All":
                result = CreativeItemCategory.All;
                return true;
            case "Construction":
                result = CreativeItemCategory.Construction;
                return true;
            case "Nature":
                result = CreativeItemCategory.Nature;
                return true;
            case "Equipment":
                result = CreativeItemCategory.Equipment;
                return true;
            case "Items":
                result = CreativeItemCategory.Items;
                return true;
            case "ItemCommandOnly":
                result = CreativeItemCategory.ItemCommandOnly;
                return true;
            case "Undefined":
                result = CreativeItemCategory.Undefined;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
