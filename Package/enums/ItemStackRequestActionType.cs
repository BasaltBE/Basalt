#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ItemStackRequestActionType {
    Take = 0,
    Place = 1,
    Swap = 2,
    Drop = 3,
    Destroy = 4,
    Consume = 5,
    Create = 6,
    ScreenLabTableCombine = 9,
    ScreenBeaconPayment = 10,
    ScreenHUDMineBlock = 11,
    CraftRecipe = 12,
    CraftRecipeAuto = 13,
    CraftCreative = 14,
    CraftRecipeOptional = 15,
    CraftRepairAndDisenchant = 16,
    CraftLoom = 17,
    CraftNonImplemented = 18,
    CraftResults = 19,
}

public static class ItemStackRequestActionTypeExtensions {
    public static string ToProtoString(this ItemStackRequestActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemStackRequestActionType value) {
        return value switch {
            ItemStackRequestActionType.Take => "Take",
            ItemStackRequestActionType.Place => "Place",
            ItemStackRequestActionType.Swap => "Swap",
            ItemStackRequestActionType.Drop => "Drop",
            ItemStackRequestActionType.Destroy => "Destroy",
            ItemStackRequestActionType.Consume => "Consume",
            ItemStackRequestActionType.Create => "Create",
            ItemStackRequestActionType.ScreenLabTableCombine => "ScreenLabTableCombine",
            ItemStackRequestActionType.ScreenBeaconPayment => "ScreenBeaconPayment",
            ItemStackRequestActionType.ScreenHUDMineBlock => "ScreenHUDMineBlock",
            ItemStackRequestActionType.CraftRecipe => "CraftRecipe",
            ItemStackRequestActionType.CraftRecipeAuto => "CraftRecipeAuto",
            ItemStackRequestActionType.CraftCreative => "CraftCreative",
            ItemStackRequestActionType.CraftRecipeOptional => "CraftRecipeOptional",
            ItemStackRequestActionType.CraftRepairAndDisenchant => "CraftRepairAndDisenchant",
            ItemStackRequestActionType.CraftLoom => "CraftLoom",
            ItemStackRequestActionType.CraftNonImplemented => "CraftNonImplemented",
            ItemStackRequestActionType.CraftResults => "CraftResults",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemStackRequestActionType value.")
        };
    }

    public static ItemStackRequestActionType FromProtocolString(string value) {
        return value switch {
            "Take" => ItemStackRequestActionType.Take,
            "Place" => ItemStackRequestActionType.Place,
            "Swap" => ItemStackRequestActionType.Swap,
            "Drop" => ItemStackRequestActionType.Drop,
            "Destroy" => ItemStackRequestActionType.Destroy,
            "Consume" => ItemStackRequestActionType.Consume,
            "Create" => ItemStackRequestActionType.Create,
            "ScreenLabTableCombine" => ItemStackRequestActionType.ScreenLabTableCombine,
            "ScreenBeaconPayment" => ItemStackRequestActionType.ScreenBeaconPayment,
            "ScreenHUDMineBlock" => ItemStackRequestActionType.ScreenHUDMineBlock,
            "CraftRecipe" => ItemStackRequestActionType.CraftRecipe,
            "CraftRecipeAuto" => ItemStackRequestActionType.CraftRecipeAuto,
            "CraftCreative" => ItemStackRequestActionType.CraftCreative,
            "CraftRecipeOptional" => ItemStackRequestActionType.CraftRecipeOptional,
            "CraftRepairAndDisenchant" => ItemStackRequestActionType.CraftRepairAndDisenchant,
            "CraftLoom" => ItemStackRequestActionType.CraftLoom,
            "CraftNonImplemented" => ItemStackRequestActionType.CraftNonImplemented,
            "CraftResults" => ItemStackRequestActionType.CraftResults,
            _ => throw new ArgumentException($"Unknown ItemStackRequestActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemStackRequestActionType result) {
        switch (value) {
            case "Take":
                result = ItemStackRequestActionType.Take;
                return true;
            case "Place":
                result = ItemStackRequestActionType.Place;
                return true;
            case "Swap":
                result = ItemStackRequestActionType.Swap;
                return true;
            case "Drop":
                result = ItemStackRequestActionType.Drop;
                return true;
            case "Destroy":
                result = ItemStackRequestActionType.Destroy;
                return true;
            case "Consume":
                result = ItemStackRequestActionType.Consume;
                return true;
            case "Create":
                result = ItemStackRequestActionType.Create;
                return true;
            case "ScreenLabTableCombine":
                result = ItemStackRequestActionType.ScreenLabTableCombine;
                return true;
            case "ScreenBeaconPayment":
                result = ItemStackRequestActionType.ScreenBeaconPayment;
                return true;
            case "ScreenHUDMineBlock":
                result = ItemStackRequestActionType.ScreenHUDMineBlock;
                return true;
            case "CraftRecipe":
                result = ItemStackRequestActionType.CraftRecipe;
                return true;
            case "CraftRecipeAuto":
                result = ItemStackRequestActionType.CraftRecipeAuto;
                return true;
            case "CraftCreative":
                result = ItemStackRequestActionType.CraftCreative;
                return true;
            case "CraftRecipeOptional":
                result = ItemStackRequestActionType.CraftRecipeOptional;
                return true;
            case "CraftRepairAndDisenchant":
                result = ItemStackRequestActionType.CraftRepairAndDisenchant;
                return true;
            case "CraftLoom":
                result = ItemStackRequestActionType.CraftLoom;
                return true;
            case "CraftNonImplemented":
                result = ItemStackRequestActionType.CraftNonImplemented;
                return true;
            case "CraftResults":
                result = ItemStackRequestActionType.CraftResults;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
