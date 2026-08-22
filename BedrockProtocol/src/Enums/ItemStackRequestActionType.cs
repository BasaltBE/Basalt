namespace Basalt.BedrockProtocol.Enums;

public enum ItemStackRequestActionType : byte {
    Take,
    Place,
    Swap,
    Drop,
    Destroy,
    Consume,
    Create,
    PlaceInItemContainer,
    TakeFromItemContainer,
    ScreenLabTableCombine,
    ScreenBeaconPayment,
    ScreenHUDMineBlock,
    CraftRecipe,
    CraftRecipeAuto,
    CraftCreative,
    CraftRecipeOptional,
    CraftRepairAndDisenchant,
    CraftLoom,
    CraftNonImplemented,
    CraftResults
}
