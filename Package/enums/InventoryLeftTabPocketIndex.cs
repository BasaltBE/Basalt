#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum InventoryLeftTabPocketIndex {
    None = 0,
    Survival = 1,
    RecipeNature = 2,
    RecipeItems = 3,
    RecipeEquipment = 4,
    RecipeConstruction = 5,
    RecipeSearch = 6,
}

public static class InventoryLeftTabPocketIndexExtensions {
    public static string ToProtoString(this InventoryLeftTabPocketIndex value) => value.ToProtocolString();

    public static string ToProtocolString(this InventoryLeftTabPocketIndex value) {
        return value switch {
            InventoryLeftTabPocketIndex.None => "None",
            InventoryLeftTabPocketIndex.Survival => "Survival",
            InventoryLeftTabPocketIndex.RecipeNature => "RecipeNature",
            InventoryLeftTabPocketIndex.RecipeItems => "RecipeItems",
            InventoryLeftTabPocketIndex.RecipeEquipment => "RecipeEquipment",
            InventoryLeftTabPocketIndex.RecipeConstruction => "RecipeConstruction",
            InventoryLeftTabPocketIndex.RecipeSearch => "RecipeSearch",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventoryLeftTabPocketIndex value.")
        };
    }

    public static InventoryLeftTabPocketIndex FromProtocolString(string value) {
        return value switch {
            "None" => InventoryLeftTabPocketIndex.None,
            "Survival" => InventoryLeftTabPocketIndex.Survival,
            "RecipeNature" => InventoryLeftTabPocketIndex.RecipeNature,
            "RecipeItems" => InventoryLeftTabPocketIndex.RecipeItems,
            "RecipeEquipment" => InventoryLeftTabPocketIndex.RecipeEquipment,
            "RecipeConstruction" => InventoryLeftTabPocketIndex.RecipeConstruction,
            "RecipeSearch" => InventoryLeftTabPocketIndex.RecipeSearch,
            _ => throw new ArgumentException($"Unknown InventoryLeftTabPocketIndex protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventoryLeftTabPocketIndex result) {
        switch (value) {
            case "None":
                result = InventoryLeftTabPocketIndex.None;
                return true;
            case "Survival":
                result = InventoryLeftTabPocketIndex.Survival;
                return true;
            case "RecipeNature":
                result = InventoryLeftTabPocketIndex.RecipeNature;
                return true;
            case "RecipeItems":
                result = InventoryLeftTabPocketIndex.RecipeItems;
                return true;
            case "RecipeEquipment":
                result = InventoryLeftTabPocketIndex.RecipeEquipment;
                return true;
            case "RecipeConstruction":
                result = InventoryLeftTabPocketIndex.RecipeConstruction;
                return true;
            case "RecipeSearch":
                result = InventoryLeftTabPocketIndex.RecipeSearch;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
