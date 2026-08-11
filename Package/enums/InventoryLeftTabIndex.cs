using System;

namespace BedrockProtocol.Enums;

public enum InventoryLeftTabIndex {
    None = 0,
    RecipeConstruction = 1,
    RecipeEquipment = 2,
    RecipeItems = 3,
    RecipeNature = 4,
    RecipeSearch = 5,
    Survival = 6,
}

public static class InventoryLeftTabIndexExtensions {
    public static string ToProtoString(this InventoryLeftTabIndex value) => value.ToProtocolString();

    public static string ToProtocolString(this InventoryLeftTabIndex value) {
        return value switch {
            InventoryLeftTabIndex.None => "None",
            InventoryLeftTabIndex.RecipeConstruction => "RecipeConstruction",
            InventoryLeftTabIndex.RecipeEquipment => "RecipeEquipment",
            InventoryLeftTabIndex.RecipeItems => "RecipeItems",
            InventoryLeftTabIndex.RecipeNature => "RecipeNature",
            InventoryLeftTabIndex.RecipeSearch => "RecipeSearch",
            InventoryLeftTabIndex.Survival => "Survival",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventoryLeftTabIndex value.")
        };
    }

    public static InventoryLeftTabIndex FromProtocolString(string value) {
        return value switch {
            "None" => InventoryLeftTabIndex.None,
            "RecipeConstruction" => InventoryLeftTabIndex.RecipeConstruction,
            "RecipeEquipment" => InventoryLeftTabIndex.RecipeEquipment,
            "RecipeItems" => InventoryLeftTabIndex.RecipeItems,
            "RecipeNature" => InventoryLeftTabIndex.RecipeNature,
            "RecipeSearch" => InventoryLeftTabIndex.RecipeSearch,
            "Survival" => InventoryLeftTabIndex.Survival,
            _ => throw new ArgumentException($"Unknown InventoryLeftTabIndex protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventoryLeftTabIndex result) {
        switch (value) {
            case "None":
                result = InventoryLeftTabIndex.None;
                return true;
            case "RecipeConstruction":
                result = InventoryLeftTabIndex.RecipeConstruction;
                return true;
            case "RecipeEquipment":
                result = InventoryLeftTabIndex.RecipeEquipment;
                return true;
            case "RecipeItems":
                result = InventoryLeftTabIndex.RecipeItems;
                return true;
            case "RecipeNature":
                result = InventoryLeftTabIndex.RecipeNature;
                return true;
            case "RecipeSearch":
                result = InventoryLeftTabIndex.RecipeSearch;
                return true;
            case "Survival":
                result = InventoryLeftTabIndex.Survival;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
