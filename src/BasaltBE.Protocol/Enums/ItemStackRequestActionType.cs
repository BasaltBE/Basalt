namespace BasaltBE.Protocol.Enums;

public enum ItemStackRequestActionType : byte {
    Take = 0,
    Place = 1,
    Swap = 2,
    Drop = 3,
    Destroy = 4,
    Consume = 5,
    Create = 6,
    PlaceInItemContainer = 7,
    TakeFromItemContainer = 8,
    ScreenLabTableCombine = 9,
    ScreenBeaconPayment = 10,
    ScreenHudMineBlock = 11,
    CraftRecipe = 12,
    CraftRecipeAuto = 13,
    CraftCreative = 14,
    CraftRecipeOptional = 15,
    CraftRepairAndDisenchant = 16,
    CraftLoom = 17,
    CraftReservedAction = 18,
    CraftNonImplemented = 19,
    CraftResults = 20
}
