using System;

namespace BedrockProtocol.Enums;

public enum ContainerEnumName {
    AnvilInputContainer = 0,
    AnvilMaterialContainer = 1,
    AnvilResultPreviewContainer = 2,
    SmithingTableInputContainer = 3,
    SmithingTableMaterialContainer = 4,
    SmithingTableResultPreviewContainer = 5,
    ArmorContainer = 6,
    LevelEntityContainer = 7,
    BeaconPaymentContainer = 8,
    BrewingStandInputContainer = 9,
    BrewingStandResultContainer = 10,
    BrewingStandFuelContainer = 11,
    CombinedHotbarAndInventoryContainer = 12,
    CraftingInputContainer = 13,
    CraftingOutputPreviewContainer = 14,
    RecipeConstructionContainer = 15,
    RecipeNatureContainer = 16,
    RecipeItemsContainer = 17,
    RecipeFoodContainer = 64,
    RecipeBlocksContainer = 65,
    RecipeFurnaceItemsContainer = 66,
    RecipeSearchContainer = 18,
    RecipeSearchBarContainer = 19,
    RecipeEquipmentContainer = 20,
    RecipeBookContainer = 21,
    EnchantingInputContainer = 22,
    EnchantingMaterialContainer = 23,
    FurnaceFuelContainer = 24,
    FurnaceIngredientContainer = 25,
    FurnaceResultContainer = 26,
    HorseEquipContainer = 27,
    HotbarContainer = 28,
    InventoryContainer = 29,
    ShulkerBoxContainer = 30,
    TradeIngredient1Container = 31,
    TradeIngredient2Container = 32,
    TradeResultPreviewContainer = 33,
    OffhandContainer = 34,
    CompoundCreatorInput = 35,
    CompoundCreatorOutputPreview = 36,
    ElementConstructorOutputPreview = 37,
    MaterialReducerInput = 38,
    MaterialReducerOutput = 39,
    LabTableInput = 40,
    LoomInputContainer = 41,
    LoomDyeContainer = 42,
    LoomMaterialContainer = 43,
    LoomResultPreviewContainer = 44,
    BlastFurnaceIngredientContainer = 45,
    SmokerIngredientContainer = 46,
    Trade2Ingredient1Container = 47,
    Trade2Ingredient2Container = 48,
    Trade2ResultPreviewContainer = 49,
    GrindstoneInputContainer = 50,
    GrindstoneAdditionalContainer = 51,
    GrindstoneResultPreviewContainer = 52,
    StonecutterInputContainer = 53,
    StonecutterResultPreviewContainer = 54,
    CartographyInputContainer = 55,
    CartographyAdditionalContainer = 56,
    CartographyResultPreviewContainer = 57,
    BarrelContainer = 58,
    CursorContainer = 59,
    CreatedOutputContainer = 60,
    SmithingTableTemplateContainer = 61,
    CrafterLevelEntityContainer = 62,
    DynamicContainer = 63,
}

public static class ContainerEnumNameExtensions {
    public static string ToProtoString(this ContainerEnumName value) => value.ToProtocolString();

    public static string ToProtocolString(this ContainerEnumName value) {
        return value switch {
            ContainerEnumName.AnvilInputContainer => "AnvilInputContainer",
            ContainerEnumName.AnvilMaterialContainer => "AnvilMaterialContainer",
            ContainerEnumName.AnvilResultPreviewContainer => "AnvilResultPreviewContainer",
            ContainerEnumName.SmithingTableInputContainer => "SmithingTableInputContainer",
            ContainerEnumName.SmithingTableMaterialContainer => "SmithingTableMaterialContainer",
            ContainerEnumName.SmithingTableResultPreviewContainer => "SmithingTableResultPreviewContainer",
            ContainerEnumName.ArmorContainer => "ArmorContainer",
            ContainerEnumName.LevelEntityContainer => "LevelEntityContainer",
            ContainerEnumName.BeaconPaymentContainer => "BeaconPaymentContainer",
            ContainerEnumName.BrewingStandInputContainer => "BrewingStandInputContainer",
            ContainerEnumName.BrewingStandResultContainer => "BrewingStandResultContainer",
            ContainerEnumName.BrewingStandFuelContainer => "BrewingStandFuelContainer",
            ContainerEnumName.CombinedHotbarAndInventoryContainer => "CombinedHotbarAndInventoryContainer",
            ContainerEnumName.CraftingInputContainer => "CraftingInputContainer",
            ContainerEnumName.CraftingOutputPreviewContainer => "CraftingOutputPreviewContainer",
            ContainerEnumName.RecipeConstructionContainer => "RecipeConstructionContainer",
            ContainerEnumName.RecipeNatureContainer => "RecipeNatureContainer",
            ContainerEnumName.RecipeItemsContainer => "RecipeItemsContainer",
            ContainerEnumName.RecipeFoodContainer => "RecipeFoodContainer",
            ContainerEnumName.RecipeBlocksContainer => "RecipeBlocksContainer",
            ContainerEnumName.RecipeFurnaceItemsContainer => "RecipeFurnaceItemsContainer",
            ContainerEnumName.RecipeSearchContainer => "RecipeSearchContainer",
            ContainerEnumName.RecipeSearchBarContainer => "RecipeSearchBarContainer",
            ContainerEnumName.RecipeEquipmentContainer => "RecipeEquipmentContainer",
            ContainerEnumName.RecipeBookContainer => "RecipeBookContainer",
            ContainerEnumName.EnchantingInputContainer => "EnchantingInputContainer",
            ContainerEnumName.EnchantingMaterialContainer => "EnchantingMaterialContainer",
            ContainerEnumName.FurnaceFuelContainer => "FurnaceFuelContainer",
            ContainerEnumName.FurnaceIngredientContainer => "FurnaceIngredientContainer",
            ContainerEnumName.FurnaceResultContainer => "FurnaceResultContainer",
            ContainerEnumName.HorseEquipContainer => "HorseEquipContainer",
            ContainerEnumName.HotbarContainer => "HotbarContainer",
            ContainerEnumName.InventoryContainer => "InventoryContainer",
            ContainerEnumName.ShulkerBoxContainer => "ShulkerBoxContainer",
            ContainerEnumName.TradeIngredient1Container => "TradeIngredient1Container",
            ContainerEnumName.TradeIngredient2Container => "TradeIngredient2Container",
            ContainerEnumName.TradeResultPreviewContainer => "TradeResultPreviewContainer",
            ContainerEnumName.OffhandContainer => "OffhandContainer",
            ContainerEnumName.CompoundCreatorInput => "CompoundCreatorInput",
            ContainerEnumName.CompoundCreatorOutputPreview => "CompoundCreatorOutputPreview",
            ContainerEnumName.ElementConstructorOutputPreview => "ElementConstructorOutputPreview",
            ContainerEnumName.MaterialReducerInput => "MaterialReducerInput",
            ContainerEnumName.MaterialReducerOutput => "MaterialReducerOutput",
            ContainerEnumName.LabTableInput => "LabTableInput",
            ContainerEnumName.LoomInputContainer => "LoomInputContainer",
            ContainerEnumName.LoomDyeContainer => "LoomDyeContainer",
            ContainerEnumName.LoomMaterialContainer => "LoomMaterialContainer",
            ContainerEnumName.LoomResultPreviewContainer => "LoomResultPreviewContainer",
            ContainerEnumName.BlastFurnaceIngredientContainer => "BlastFurnaceIngredientContainer",
            ContainerEnumName.SmokerIngredientContainer => "SmokerIngredientContainer",
            ContainerEnumName.Trade2Ingredient1Container => "Trade2Ingredient1Container",
            ContainerEnumName.Trade2Ingredient2Container => "Trade2Ingredient2Container",
            ContainerEnumName.Trade2ResultPreviewContainer => "Trade2ResultPreviewContainer",
            ContainerEnumName.GrindstoneInputContainer => "GrindstoneInputContainer",
            ContainerEnumName.GrindstoneAdditionalContainer => "GrindstoneAdditionalContainer",
            ContainerEnumName.GrindstoneResultPreviewContainer => "GrindstoneResultPreviewContainer",
            ContainerEnumName.StonecutterInputContainer => "StonecutterInputContainer",
            ContainerEnumName.StonecutterResultPreviewContainer => "StonecutterResultPreviewContainer",
            ContainerEnumName.CartographyInputContainer => "CartographyInputContainer",
            ContainerEnumName.CartographyAdditionalContainer => "CartographyAdditionalContainer",
            ContainerEnumName.CartographyResultPreviewContainer => "CartographyResultPreviewContainer",
            ContainerEnumName.BarrelContainer => "BarrelContainer",
            ContainerEnumName.CursorContainer => "CursorContainer",
            ContainerEnumName.CreatedOutputContainer => "CreatedOutputContainer",
            ContainerEnumName.SmithingTableTemplateContainer => "SmithingTableTemplateContainer",
            ContainerEnumName.CrafterLevelEntityContainer => "CrafterLevelEntityContainer",
            ContainerEnumName.DynamicContainer => "DynamicContainer",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ContainerEnumName value.")
        };
    }

    public static ContainerEnumName FromProtocolString(string value) {
        return value switch {
            "AnvilInputContainer" => ContainerEnumName.AnvilInputContainer,
            "AnvilMaterialContainer" => ContainerEnumName.AnvilMaterialContainer,
            "AnvilResultPreviewContainer" => ContainerEnumName.AnvilResultPreviewContainer,
            "SmithingTableInputContainer" => ContainerEnumName.SmithingTableInputContainer,
            "SmithingTableMaterialContainer" => ContainerEnumName.SmithingTableMaterialContainer,
            "SmithingTableResultPreviewContainer" => ContainerEnumName.SmithingTableResultPreviewContainer,
            "ArmorContainer" => ContainerEnumName.ArmorContainer,
            "LevelEntityContainer" => ContainerEnumName.LevelEntityContainer,
            "BeaconPaymentContainer" => ContainerEnumName.BeaconPaymentContainer,
            "BrewingStandInputContainer" => ContainerEnumName.BrewingStandInputContainer,
            "BrewingStandResultContainer" => ContainerEnumName.BrewingStandResultContainer,
            "BrewingStandFuelContainer" => ContainerEnumName.BrewingStandFuelContainer,
            "CombinedHotbarAndInventoryContainer" => ContainerEnumName.CombinedHotbarAndInventoryContainer,
            "CraftingInputContainer" => ContainerEnumName.CraftingInputContainer,
            "CraftingOutputPreviewContainer" => ContainerEnumName.CraftingOutputPreviewContainer,
            "RecipeConstructionContainer" => ContainerEnumName.RecipeConstructionContainer,
            "RecipeNatureContainer" => ContainerEnumName.RecipeNatureContainer,
            "RecipeItemsContainer" => ContainerEnumName.RecipeItemsContainer,
            "RecipeFoodContainer" => ContainerEnumName.RecipeFoodContainer,
            "RecipeBlocksContainer" => ContainerEnumName.RecipeBlocksContainer,
            "RecipeFurnaceItemsContainer" => ContainerEnumName.RecipeFurnaceItemsContainer,
            "RecipeSearchContainer" => ContainerEnumName.RecipeSearchContainer,
            "RecipeSearchBarContainer" => ContainerEnumName.RecipeSearchBarContainer,
            "RecipeEquipmentContainer" => ContainerEnumName.RecipeEquipmentContainer,
            "RecipeBookContainer" => ContainerEnumName.RecipeBookContainer,
            "EnchantingInputContainer" => ContainerEnumName.EnchantingInputContainer,
            "EnchantingMaterialContainer" => ContainerEnumName.EnchantingMaterialContainer,
            "FurnaceFuelContainer" => ContainerEnumName.FurnaceFuelContainer,
            "FurnaceIngredientContainer" => ContainerEnumName.FurnaceIngredientContainer,
            "FurnaceResultContainer" => ContainerEnumName.FurnaceResultContainer,
            "HorseEquipContainer" => ContainerEnumName.HorseEquipContainer,
            "HotbarContainer" => ContainerEnumName.HotbarContainer,
            "InventoryContainer" => ContainerEnumName.InventoryContainer,
            "ShulkerBoxContainer" => ContainerEnumName.ShulkerBoxContainer,
            "TradeIngredient1Container" => ContainerEnumName.TradeIngredient1Container,
            "TradeIngredient2Container" => ContainerEnumName.TradeIngredient2Container,
            "TradeResultPreviewContainer" => ContainerEnumName.TradeResultPreviewContainer,
            "OffhandContainer" => ContainerEnumName.OffhandContainer,
            "CompoundCreatorInput" => ContainerEnumName.CompoundCreatorInput,
            "CompoundCreatorOutputPreview" => ContainerEnumName.CompoundCreatorOutputPreview,
            "ElementConstructorOutputPreview" => ContainerEnumName.ElementConstructorOutputPreview,
            "MaterialReducerInput" => ContainerEnumName.MaterialReducerInput,
            "MaterialReducerOutput" => ContainerEnumName.MaterialReducerOutput,
            "LabTableInput" => ContainerEnumName.LabTableInput,
            "LoomInputContainer" => ContainerEnumName.LoomInputContainer,
            "LoomDyeContainer" => ContainerEnumName.LoomDyeContainer,
            "LoomMaterialContainer" => ContainerEnumName.LoomMaterialContainer,
            "LoomResultPreviewContainer" => ContainerEnumName.LoomResultPreviewContainer,
            "BlastFurnaceIngredientContainer" => ContainerEnumName.BlastFurnaceIngredientContainer,
            "SmokerIngredientContainer" => ContainerEnumName.SmokerIngredientContainer,
            "Trade2Ingredient1Container" => ContainerEnumName.Trade2Ingredient1Container,
            "Trade2Ingredient2Container" => ContainerEnumName.Trade2Ingredient2Container,
            "Trade2ResultPreviewContainer" => ContainerEnumName.Trade2ResultPreviewContainer,
            "GrindstoneInputContainer" => ContainerEnumName.GrindstoneInputContainer,
            "GrindstoneAdditionalContainer" => ContainerEnumName.GrindstoneAdditionalContainer,
            "GrindstoneResultPreviewContainer" => ContainerEnumName.GrindstoneResultPreviewContainer,
            "StonecutterInputContainer" => ContainerEnumName.StonecutterInputContainer,
            "StonecutterResultPreviewContainer" => ContainerEnumName.StonecutterResultPreviewContainer,
            "CartographyInputContainer" => ContainerEnumName.CartographyInputContainer,
            "CartographyAdditionalContainer" => ContainerEnumName.CartographyAdditionalContainer,
            "CartographyResultPreviewContainer" => ContainerEnumName.CartographyResultPreviewContainer,
            "BarrelContainer" => ContainerEnumName.BarrelContainer,
            "CursorContainer" => ContainerEnumName.CursorContainer,
            "CreatedOutputContainer" => ContainerEnumName.CreatedOutputContainer,
            "SmithingTableTemplateContainer" => ContainerEnumName.SmithingTableTemplateContainer,
            "CrafterLevelEntityContainer" => ContainerEnumName.CrafterLevelEntityContainer,
            "DynamicContainer" => ContainerEnumName.DynamicContainer,
            _ => throw new ArgumentException($"Unknown ContainerEnumName protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ContainerEnumName result) {
        switch (value) {
            case "AnvilInputContainer":
                result = ContainerEnumName.AnvilInputContainer;
                return true;
            case "AnvilMaterialContainer":
                result = ContainerEnumName.AnvilMaterialContainer;
                return true;
            case "AnvilResultPreviewContainer":
                result = ContainerEnumName.AnvilResultPreviewContainer;
                return true;
            case "SmithingTableInputContainer":
                result = ContainerEnumName.SmithingTableInputContainer;
                return true;
            case "SmithingTableMaterialContainer":
                result = ContainerEnumName.SmithingTableMaterialContainer;
                return true;
            case "SmithingTableResultPreviewContainer":
                result = ContainerEnumName.SmithingTableResultPreviewContainer;
                return true;
            case "ArmorContainer":
                result = ContainerEnumName.ArmorContainer;
                return true;
            case "LevelEntityContainer":
                result = ContainerEnumName.LevelEntityContainer;
                return true;
            case "BeaconPaymentContainer":
                result = ContainerEnumName.BeaconPaymentContainer;
                return true;
            case "BrewingStandInputContainer":
                result = ContainerEnumName.BrewingStandInputContainer;
                return true;
            case "BrewingStandResultContainer":
                result = ContainerEnumName.BrewingStandResultContainer;
                return true;
            case "BrewingStandFuelContainer":
                result = ContainerEnumName.BrewingStandFuelContainer;
                return true;
            case "CombinedHotbarAndInventoryContainer":
                result = ContainerEnumName.CombinedHotbarAndInventoryContainer;
                return true;
            case "CraftingInputContainer":
                result = ContainerEnumName.CraftingInputContainer;
                return true;
            case "CraftingOutputPreviewContainer":
                result = ContainerEnumName.CraftingOutputPreviewContainer;
                return true;
            case "RecipeConstructionContainer":
                result = ContainerEnumName.RecipeConstructionContainer;
                return true;
            case "RecipeNatureContainer":
                result = ContainerEnumName.RecipeNatureContainer;
                return true;
            case "RecipeItemsContainer":
                result = ContainerEnumName.RecipeItemsContainer;
                return true;
            case "RecipeFoodContainer":
                result = ContainerEnumName.RecipeFoodContainer;
                return true;
            case "RecipeBlocksContainer":
                result = ContainerEnumName.RecipeBlocksContainer;
                return true;
            case "RecipeFurnaceItemsContainer":
                result = ContainerEnumName.RecipeFurnaceItemsContainer;
                return true;
            case "RecipeSearchContainer":
                result = ContainerEnumName.RecipeSearchContainer;
                return true;
            case "RecipeSearchBarContainer":
                result = ContainerEnumName.RecipeSearchBarContainer;
                return true;
            case "RecipeEquipmentContainer":
                result = ContainerEnumName.RecipeEquipmentContainer;
                return true;
            case "RecipeBookContainer":
                result = ContainerEnumName.RecipeBookContainer;
                return true;
            case "EnchantingInputContainer":
                result = ContainerEnumName.EnchantingInputContainer;
                return true;
            case "EnchantingMaterialContainer":
                result = ContainerEnumName.EnchantingMaterialContainer;
                return true;
            case "FurnaceFuelContainer":
                result = ContainerEnumName.FurnaceFuelContainer;
                return true;
            case "FurnaceIngredientContainer":
                result = ContainerEnumName.FurnaceIngredientContainer;
                return true;
            case "FurnaceResultContainer":
                result = ContainerEnumName.FurnaceResultContainer;
                return true;
            case "HorseEquipContainer":
                result = ContainerEnumName.HorseEquipContainer;
                return true;
            case "HotbarContainer":
                result = ContainerEnumName.HotbarContainer;
                return true;
            case "InventoryContainer":
                result = ContainerEnumName.InventoryContainer;
                return true;
            case "ShulkerBoxContainer":
                result = ContainerEnumName.ShulkerBoxContainer;
                return true;
            case "TradeIngredient1Container":
                result = ContainerEnumName.TradeIngredient1Container;
                return true;
            case "TradeIngredient2Container":
                result = ContainerEnumName.TradeIngredient2Container;
                return true;
            case "TradeResultPreviewContainer":
                result = ContainerEnumName.TradeResultPreviewContainer;
                return true;
            case "OffhandContainer":
                result = ContainerEnumName.OffhandContainer;
                return true;
            case "CompoundCreatorInput":
                result = ContainerEnumName.CompoundCreatorInput;
                return true;
            case "CompoundCreatorOutputPreview":
                result = ContainerEnumName.CompoundCreatorOutputPreview;
                return true;
            case "ElementConstructorOutputPreview":
                result = ContainerEnumName.ElementConstructorOutputPreview;
                return true;
            case "MaterialReducerInput":
                result = ContainerEnumName.MaterialReducerInput;
                return true;
            case "MaterialReducerOutput":
                result = ContainerEnumName.MaterialReducerOutput;
                return true;
            case "LabTableInput":
                result = ContainerEnumName.LabTableInput;
                return true;
            case "LoomInputContainer":
                result = ContainerEnumName.LoomInputContainer;
                return true;
            case "LoomDyeContainer":
                result = ContainerEnumName.LoomDyeContainer;
                return true;
            case "LoomMaterialContainer":
                result = ContainerEnumName.LoomMaterialContainer;
                return true;
            case "LoomResultPreviewContainer":
                result = ContainerEnumName.LoomResultPreviewContainer;
                return true;
            case "BlastFurnaceIngredientContainer":
                result = ContainerEnumName.BlastFurnaceIngredientContainer;
                return true;
            case "SmokerIngredientContainer":
                result = ContainerEnumName.SmokerIngredientContainer;
                return true;
            case "Trade2Ingredient1Container":
                result = ContainerEnumName.Trade2Ingredient1Container;
                return true;
            case "Trade2Ingredient2Container":
                result = ContainerEnumName.Trade2Ingredient2Container;
                return true;
            case "Trade2ResultPreviewContainer":
                result = ContainerEnumName.Trade2ResultPreviewContainer;
                return true;
            case "GrindstoneInputContainer":
                result = ContainerEnumName.GrindstoneInputContainer;
                return true;
            case "GrindstoneAdditionalContainer":
                result = ContainerEnumName.GrindstoneAdditionalContainer;
                return true;
            case "GrindstoneResultPreviewContainer":
                result = ContainerEnumName.GrindstoneResultPreviewContainer;
                return true;
            case "StonecutterInputContainer":
                result = ContainerEnumName.StonecutterInputContainer;
                return true;
            case "StonecutterResultPreviewContainer":
                result = ContainerEnumName.StonecutterResultPreviewContainer;
                return true;
            case "CartographyInputContainer":
                result = ContainerEnumName.CartographyInputContainer;
                return true;
            case "CartographyAdditionalContainer":
                result = ContainerEnumName.CartographyAdditionalContainer;
                return true;
            case "CartographyResultPreviewContainer":
                result = ContainerEnumName.CartographyResultPreviewContainer;
                return true;
            case "BarrelContainer":
                result = ContainerEnumName.BarrelContainer;
                return true;
            case "CursorContainer":
                result = ContainerEnumName.CursorContainer;
                return true;
            case "CreatedOutputContainer":
                result = ContainerEnumName.CreatedOutputContainer;
                return true;
            case "SmithingTableTemplateContainer":
                result = ContainerEnumName.SmithingTableTemplateContainer;
                return true;
            case "CrafterLevelEntityContainer":
                result = ContainerEnumName.CrafterLevelEntityContainer;
                return true;
            case "DynamicContainer":
                result = ContainerEnumName.DynamicContainer;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
