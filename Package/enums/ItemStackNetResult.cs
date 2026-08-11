using System;

namespace BedrockProtocol.Enums;

public enum ItemStackNetResult {
    Success = 0,
    Error = 1,
    InvalidRequestActionType = 2,
    ActionRequestNotAllowed = 3,
    ScreenHandlerEndRequestFailed = 4,
    ItemRequestActionHandlerCommitFailed = 5,
    InvalidRequestCraftActionType = 6,
    InvalidCraftRequest = 7,
    InvalidCraftRequestScreen = 8,
    InvalidCraftResult = 9,
    InvalidCraftResultIndex = 10,
    InvalidCraftResultItem = 11,
    InvalidItemNetId = 12,
    MissingCreatedOutputContainer = 13,
    FailedToSetCreatedItemOutputSlot = 14,
    RequestAlreadyInProgress = 15,
    FailedToInitSparseContainer = 16,
    ResultTransferFailed = 17,
    ExpectedItemSlotNotFullyConsumed = 18,
    ExpectedAnywhereItemNotFullyConsumed = 19,
    ItemAlreadyConsumedFromSlot = 20,
    ConsumedTooMuchFromSlot = 21,
    MismatchSlotExpectedConsumedItem = 22,
    MismatchSlotExpectedConsumedItemNetIdVariant = 23,
    FailedToMatchExpectedSlotConsumedItem = 24,
    FailedToMatchExpectedAllowedAnywhereConsumedItem = 25,
    ConsumedItemOutOfAllowedSlotRange = 26,
    ConsumedItemNotAllowed = 27,
    PlayerNotInCreativeMode = 28,
    InvalidExperimentalRecipeRequest = 29,
    FailedToCraftCreative = 30,
    FailedToGetLevelRecipe = 31,
    FailedToFindRecipeByNetId = 32,
    MismatchedCraftingSize = 33,
    MissingInputSparseContainer = 34,
    MismatchedRecipeForInputGridItems = 35,
    EmptyCraftResults = 36,
    FailedToEnchant = 37,
    MissingInputItem = 38,
    InsufficientPlayerLevelToEnchant = 39,
    MissingMaterialItem = 40,
    MissingActor = 41,
    UnknownPrimaryEffect = 42,
    PrimaryEffectOutOfRange = 43,
    PrimaryEffectUnavailable = 44,
    SecondaryEffectOutOfRange = 45,
    SecondaryEffectUnavailable = 46,
    DstContainerEqualToCreatedOutputContainer = 47,
    DstContainerAndSlotEqualToSrcContainerAndSlot = 48,
    FailedToValidateSrcSlot = 49,
    FailedToValidateDstSlot = 50,
    InvalidAdjustedAmount = 51,
    InvalidItemSetType = 52,
    InvalidTransferAmount = 53,
    CannotSwapItem = 54,
    CannotPlaceItem = 55,
    UnhandledItemSetType = 56,
    InvalidRemovedAmount = 57,
    InvalidRegion = 58,
    CannotDropItem = 59,
    CannotDestroyItem = 60,
    InvalidSourceContainer = 61,
    ItemNotConsumed = 62,
    InvalidNumCrafts = 63,
    InvalidCraftResultStackSize = 64,
    CannotRemoveItem = 65,
    CannotConsumeItem = 66,
    ScreenStackError = 67,
}

public static class ItemStackNetResultExtensions {
    public static string ToProtoString(this ItemStackNetResult value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemStackNetResult value) {
        return value switch {
            ItemStackNetResult.Success => "Success",
            ItemStackNetResult.Error => "Error",
            ItemStackNetResult.InvalidRequestActionType => "InvalidRequestActionType",
            ItemStackNetResult.ActionRequestNotAllowed => "ActionRequestNotAllowed",
            ItemStackNetResult.ScreenHandlerEndRequestFailed => "ScreenHandlerEndRequestFailed",
            ItemStackNetResult.ItemRequestActionHandlerCommitFailed => "ItemRequestActionHandlerCommitFailed",
            ItemStackNetResult.InvalidRequestCraftActionType => "InvalidRequestCraftActionType",
            ItemStackNetResult.InvalidCraftRequest => "InvalidCraftRequest",
            ItemStackNetResult.InvalidCraftRequestScreen => "InvalidCraftRequestScreen",
            ItemStackNetResult.InvalidCraftResult => "InvalidCraftResult",
            ItemStackNetResult.InvalidCraftResultIndex => "InvalidCraftResultIndex",
            ItemStackNetResult.InvalidCraftResultItem => "InvalidCraftResultItem",
            ItemStackNetResult.InvalidItemNetId => "InvalidItemNetId",
            ItemStackNetResult.MissingCreatedOutputContainer => "MissingCreatedOutputContainer",
            ItemStackNetResult.FailedToSetCreatedItemOutputSlot => "FailedToSetCreatedItemOutputSlot",
            ItemStackNetResult.RequestAlreadyInProgress => "RequestAlreadyInProgress",
            ItemStackNetResult.FailedToInitSparseContainer => "FailedToInitSparseContainer",
            ItemStackNetResult.ResultTransferFailed => "ResultTransferFailed",
            ItemStackNetResult.ExpectedItemSlotNotFullyConsumed => "ExpectedItemSlotNotFullyConsumed",
            ItemStackNetResult.ExpectedAnywhereItemNotFullyConsumed => "ExpectedAnywhereItemNotFullyConsumed",
            ItemStackNetResult.ItemAlreadyConsumedFromSlot => "ItemAlreadyConsumedFromSlot",
            ItemStackNetResult.ConsumedTooMuchFromSlot => "ConsumedTooMuchFromSlot",
            ItemStackNetResult.MismatchSlotExpectedConsumedItem => "MismatchSlotExpectedConsumedItem",
            ItemStackNetResult.MismatchSlotExpectedConsumedItemNetIdVariant => "MismatchSlotExpectedConsumedItemNetIdVariant",
            ItemStackNetResult.FailedToMatchExpectedSlotConsumedItem => "FailedToMatchExpectedSlotConsumedItem",
            ItemStackNetResult.FailedToMatchExpectedAllowedAnywhereConsumedItem => "FailedToMatchExpectedAllowedAnywhereConsumedItem",
            ItemStackNetResult.ConsumedItemOutOfAllowedSlotRange => "ConsumedItemOutOfAllowedSlotRange",
            ItemStackNetResult.ConsumedItemNotAllowed => "ConsumedItemNotAllowed",
            ItemStackNetResult.PlayerNotInCreativeMode => "PlayerNotInCreativeMode",
            ItemStackNetResult.InvalidExperimentalRecipeRequest => "InvalidExperimentalRecipeRequest",
            ItemStackNetResult.FailedToCraftCreative => "FailedToCraftCreative",
            ItemStackNetResult.FailedToGetLevelRecipe => "FailedToGetLevelRecipe",
            ItemStackNetResult.FailedToFindRecipeByNetId => "FailedToFindRecipeByNetId",
            ItemStackNetResult.MismatchedCraftingSize => "MismatchedCraftingSize",
            ItemStackNetResult.MissingInputSparseContainer => "MissingInputSparseContainer",
            ItemStackNetResult.MismatchedRecipeForInputGridItems => "MismatchedRecipeForInputGridItems",
            ItemStackNetResult.EmptyCraftResults => "EmptyCraftResults",
            ItemStackNetResult.FailedToEnchant => "FailedToEnchant",
            ItemStackNetResult.MissingInputItem => "MissingInputItem",
            ItemStackNetResult.InsufficientPlayerLevelToEnchant => "InsufficientPlayerLevelToEnchant",
            ItemStackNetResult.MissingMaterialItem => "MissingMaterialItem",
            ItemStackNetResult.MissingActor => "MissingActor",
            ItemStackNetResult.UnknownPrimaryEffect => "UnknownPrimaryEffect",
            ItemStackNetResult.PrimaryEffectOutOfRange => "PrimaryEffectOutOfRange",
            ItemStackNetResult.PrimaryEffectUnavailable => "PrimaryEffectUnavailable",
            ItemStackNetResult.SecondaryEffectOutOfRange => "SecondaryEffectOutOfRange",
            ItemStackNetResult.SecondaryEffectUnavailable => "SecondaryEffectUnavailable",
            ItemStackNetResult.DstContainerEqualToCreatedOutputContainer => "DstContainerEqualToCreatedOutputContainer",
            ItemStackNetResult.DstContainerAndSlotEqualToSrcContainerAndSlot => "DstContainerAndSlotEqualToSrcContainerAndSlot",
            ItemStackNetResult.FailedToValidateSrcSlot => "FailedToValidateSrcSlot",
            ItemStackNetResult.FailedToValidateDstSlot => "FailedToValidateDstSlot",
            ItemStackNetResult.InvalidAdjustedAmount => "InvalidAdjustedAmount",
            ItemStackNetResult.InvalidItemSetType => "InvalidItemSetType",
            ItemStackNetResult.InvalidTransferAmount => "InvalidTransferAmount",
            ItemStackNetResult.CannotSwapItem => "CannotSwapItem",
            ItemStackNetResult.CannotPlaceItem => "CannotPlaceItem",
            ItemStackNetResult.UnhandledItemSetType => "UnhandledItemSetType",
            ItemStackNetResult.InvalidRemovedAmount => "InvalidRemovedAmount",
            ItemStackNetResult.InvalidRegion => "InvalidRegion",
            ItemStackNetResult.CannotDropItem => "CannotDropItem",
            ItemStackNetResult.CannotDestroyItem => "CannotDestroyItem",
            ItemStackNetResult.InvalidSourceContainer => "InvalidSourceContainer",
            ItemStackNetResult.ItemNotConsumed => "ItemNotConsumed",
            ItemStackNetResult.InvalidNumCrafts => "InvalidNumCrafts",
            ItemStackNetResult.InvalidCraftResultStackSize => "InvalidCraftResultStackSize",
            ItemStackNetResult.CannotRemoveItem => "CannotRemoveItem",
            ItemStackNetResult.CannotConsumeItem => "CannotConsumeItem",
            ItemStackNetResult.ScreenStackError => "ScreenStackError",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemStackNetResult value.")
        };
    }

    public static ItemStackNetResult FromProtocolString(string value) {
        return value switch {
            "Success" => ItemStackNetResult.Success,
            "Error" => ItemStackNetResult.Error,
            "InvalidRequestActionType" => ItemStackNetResult.InvalidRequestActionType,
            "ActionRequestNotAllowed" => ItemStackNetResult.ActionRequestNotAllowed,
            "ScreenHandlerEndRequestFailed" => ItemStackNetResult.ScreenHandlerEndRequestFailed,
            "ItemRequestActionHandlerCommitFailed" => ItemStackNetResult.ItemRequestActionHandlerCommitFailed,
            "InvalidRequestCraftActionType" => ItemStackNetResult.InvalidRequestCraftActionType,
            "InvalidCraftRequest" => ItemStackNetResult.InvalidCraftRequest,
            "InvalidCraftRequestScreen" => ItemStackNetResult.InvalidCraftRequestScreen,
            "InvalidCraftResult" => ItemStackNetResult.InvalidCraftResult,
            "InvalidCraftResultIndex" => ItemStackNetResult.InvalidCraftResultIndex,
            "InvalidCraftResultItem" => ItemStackNetResult.InvalidCraftResultItem,
            "InvalidItemNetId" => ItemStackNetResult.InvalidItemNetId,
            "MissingCreatedOutputContainer" => ItemStackNetResult.MissingCreatedOutputContainer,
            "FailedToSetCreatedItemOutputSlot" => ItemStackNetResult.FailedToSetCreatedItemOutputSlot,
            "RequestAlreadyInProgress" => ItemStackNetResult.RequestAlreadyInProgress,
            "FailedToInitSparseContainer" => ItemStackNetResult.FailedToInitSparseContainer,
            "ResultTransferFailed" => ItemStackNetResult.ResultTransferFailed,
            "ExpectedItemSlotNotFullyConsumed" => ItemStackNetResult.ExpectedItemSlotNotFullyConsumed,
            "ExpectedAnywhereItemNotFullyConsumed" => ItemStackNetResult.ExpectedAnywhereItemNotFullyConsumed,
            "ItemAlreadyConsumedFromSlot" => ItemStackNetResult.ItemAlreadyConsumedFromSlot,
            "ConsumedTooMuchFromSlot" => ItemStackNetResult.ConsumedTooMuchFromSlot,
            "MismatchSlotExpectedConsumedItem" => ItemStackNetResult.MismatchSlotExpectedConsumedItem,
            "MismatchSlotExpectedConsumedItemNetIdVariant" => ItemStackNetResult.MismatchSlotExpectedConsumedItemNetIdVariant,
            "FailedToMatchExpectedSlotConsumedItem" => ItemStackNetResult.FailedToMatchExpectedSlotConsumedItem,
            "FailedToMatchExpectedAllowedAnywhereConsumedItem" => ItemStackNetResult.FailedToMatchExpectedAllowedAnywhereConsumedItem,
            "ConsumedItemOutOfAllowedSlotRange" => ItemStackNetResult.ConsumedItemOutOfAllowedSlotRange,
            "ConsumedItemNotAllowed" => ItemStackNetResult.ConsumedItemNotAllowed,
            "PlayerNotInCreativeMode" => ItemStackNetResult.PlayerNotInCreativeMode,
            "InvalidExperimentalRecipeRequest" => ItemStackNetResult.InvalidExperimentalRecipeRequest,
            "FailedToCraftCreative" => ItemStackNetResult.FailedToCraftCreative,
            "FailedToGetLevelRecipe" => ItemStackNetResult.FailedToGetLevelRecipe,
            "FailedToFindRecipeByNetId" => ItemStackNetResult.FailedToFindRecipeByNetId,
            "MismatchedCraftingSize" => ItemStackNetResult.MismatchedCraftingSize,
            "MissingInputSparseContainer" => ItemStackNetResult.MissingInputSparseContainer,
            "MismatchedRecipeForInputGridItems" => ItemStackNetResult.MismatchedRecipeForInputGridItems,
            "EmptyCraftResults" => ItemStackNetResult.EmptyCraftResults,
            "FailedToEnchant" => ItemStackNetResult.FailedToEnchant,
            "MissingInputItem" => ItemStackNetResult.MissingInputItem,
            "InsufficientPlayerLevelToEnchant" => ItemStackNetResult.InsufficientPlayerLevelToEnchant,
            "MissingMaterialItem" => ItemStackNetResult.MissingMaterialItem,
            "MissingActor" => ItemStackNetResult.MissingActor,
            "UnknownPrimaryEffect" => ItemStackNetResult.UnknownPrimaryEffect,
            "PrimaryEffectOutOfRange" => ItemStackNetResult.PrimaryEffectOutOfRange,
            "PrimaryEffectUnavailable" => ItemStackNetResult.PrimaryEffectUnavailable,
            "SecondaryEffectOutOfRange" => ItemStackNetResult.SecondaryEffectOutOfRange,
            "SecondaryEffectUnavailable" => ItemStackNetResult.SecondaryEffectUnavailable,
            "DstContainerEqualToCreatedOutputContainer" => ItemStackNetResult.DstContainerEqualToCreatedOutputContainer,
            "DstContainerAndSlotEqualToSrcContainerAndSlot" => ItemStackNetResult.DstContainerAndSlotEqualToSrcContainerAndSlot,
            "FailedToValidateSrcSlot" => ItemStackNetResult.FailedToValidateSrcSlot,
            "FailedToValidateDstSlot" => ItemStackNetResult.FailedToValidateDstSlot,
            "InvalidAdjustedAmount" => ItemStackNetResult.InvalidAdjustedAmount,
            "InvalidItemSetType" => ItemStackNetResult.InvalidItemSetType,
            "InvalidTransferAmount" => ItemStackNetResult.InvalidTransferAmount,
            "CannotSwapItem" => ItemStackNetResult.CannotSwapItem,
            "CannotPlaceItem" => ItemStackNetResult.CannotPlaceItem,
            "UnhandledItemSetType" => ItemStackNetResult.UnhandledItemSetType,
            "InvalidRemovedAmount" => ItemStackNetResult.InvalidRemovedAmount,
            "InvalidRegion" => ItemStackNetResult.InvalidRegion,
            "CannotDropItem" => ItemStackNetResult.CannotDropItem,
            "CannotDestroyItem" => ItemStackNetResult.CannotDestroyItem,
            "InvalidSourceContainer" => ItemStackNetResult.InvalidSourceContainer,
            "ItemNotConsumed" => ItemStackNetResult.ItemNotConsumed,
            "InvalidNumCrafts" => ItemStackNetResult.InvalidNumCrafts,
            "InvalidCraftResultStackSize" => ItemStackNetResult.InvalidCraftResultStackSize,
            "CannotRemoveItem" => ItemStackNetResult.CannotRemoveItem,
            "CannotConsumeItem" => ItemStackNetResult.CannotConsumeItem,
            "ScreenStackError" => ItemStackNetResult.ScreenStackError,
            _ => throw new ArgumentException($"Unknown ItemStackNetResult protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemStackNetResult result) {
        switch (value) {
            case "Success":
                result = ItemStackNetResult.Success;
                return true;
            case "Error":
                result = ItemStackNetResult.Error;
                return true;
            case "InvalidRequestActionType":
                result = ItemStackNetResult.InvalidRequestActionType;
                return true;
            case "ActionRequestNotAllowed":
                result = ItemStackNetResult.ActionRequestNotAllowed;
                return true;
            case "ScreenHandlerEndRequestFailed":
                result = ItemStackNetResult.ScreenHandlerEndRequestFailed;
                return true;
            case "ItemRequestActionHandlerCommitFailed":
                result = ItemStackNetResult.ItemRequestActionHandlerCommitFailed;
                return true;
            case "InvalidRequestCraftActionType":
                result = ItemStackNetResult.InvalidRequestCraftActionType;
                return true;
            case "InvalidCraftRequest":
                result = ItemStackNetResult.InvalidCraftRequest;
                return true;
            case "InvalidCraftRequestScreen":
                result = ItemStackNetResult.InvalidCraftRequestScreen;
                return true;
            case "InvalidCraftResult":
                result = ItemStackNetResult.InvalidCraftResult;
                return true;
            case "InvalidCraftResultIndex":
                result = ItemStackNetResult.InvalidCraftResultIndex;
                return true;
            case "InvalidCraftResultItem":
                result = ItemStackNetResult.InvalidCraftResultItem;
                return true;
            case "InvalidItemNetId":
                result = ItemStackNetResult.InvalidItemNetId;
                return true;
            case "MissingCreatedOutputContainer":
                result = ItemStackNetResult.MissingCreatedOutputContainer;
                return true;
            case "FailedToSetCreatedItemOutputSlot":
                result = ItemStackNetResult.FailedToSetCreatedItemOutputSlot;
                return true;
            case "RequestAlreadyInProgress":
                result = ItemStackNetResult.RequestAlreadyInProgress;
                return true;
            case "FailedToInitSparseContainer":
                result = ItemStackNetResult.FailedToInitSparseContainer;
                return true;
            case "ResultTransferFailed":
                result = ItemStackNetResult.ResultTransferFailed;
                return true;
            case "ExpectedItemSlotNotFullyConsumed":
                result = ItemStackNetResult.ExpectedItemSlotNotFullyConsumed;
                return true;
            case "ExpectedAnywhereItemNotFullyConsumed":
                result = ItemStackNetResult.ExpectedAnywhereItemNotFullyConsumed;
                return true;
            case "ItemAlreadyConsumedFromSlot":
                result = ItemStackNetResult.ItemAlreadyConsumedFromSlot;
                return true;
            case "ConsumedTooMuchFromSlot":
                result = ItemStackNetResult.ConsumedTooMuchFromSlot;
                return true;
            case "MismatchSlotExpectedConsumedItem":
                result = ItemStackNetResult.MismatchSlotExpectedConsumedItem;
                return true;
            case "MismatchSlotExpectedConsumedItemNetIdVariant":
                result = ItemStackNetResult.MismatchSlotExpectedConsumedItemNetIdVariant;
                return true;
            case "FailedToMatchExpectedSlotConsumedItem":
                result = ItemStackNetResult.FailedToMatchExpectedSlotConsumedItem;
                return true;
            case "FailedToMatchExpectedAllowedAnywhereConsumedItem":
                result = ItemStackNetResult.FailedToMatchExpectedAllowedAnywhereConsumedItem;
                return true;
            case "ConsumedItemOutOfAllowedSlotRange":
                result = ItemStackNetResult.ConsumedItemOutOfAllowedSlotRange;
                return true;
            case "ConsumedItemNotAllowed":
                result = ItemStackNetResult.ConsumedItemNotAllowed;
                return true;
            case "PlayerNotInCreativeMode":
                result = ItemStackNetResult.PlayerNotInCreativeMode;
                return true;
            case "InvalidExperimentalRecipeRequest":
                result = ItemStackNetResult.InvalidExperimentalRecipeRequest;
                return true;
            case "FailedToCraftCreative":
                result = ItemStackNetResult.FailedToCraftCreative;
                return true;
            case "FailedToGetLevelRecipe":
                result = ItemStackNetResult.FailedToGetLevelRecipe;
                return true;
            case "FailedToFindRecipeByNetId":
                result = ItemStackNetResult.FailedToFindRecipeByNetId;
                return true;
            case "MismatchedCraftingSize":
                result = ItemStackNetResult.MismatchedCraftingSize;
                return true;
            case "MissingInputSparseContainer":
                result = ItemStackNetResult.MissingInputSparseContainer;
                return true;
            case "MismatchedRecipeForInputGridItems":
                result = ItemStackNetResult.MismatchedRecipeForInputGridItems;
                return true;
            case "EmptyCraftResults":
                result = ItemStackNetResult.EmptyCraftResults;
                return true;
            case "FailedToEnchant":
                result = ItemStackNetResult.FailedToEnchant;
                return true;
            case "MissingInputItem":
                result = ItemStackNetResult.MissingInputItem;
                return true;
            case "InsufficientPlayerLevelToEnchant":
                result = ItemStackNetResult.InsufficientPlayerLevelToEnchant;
                return true;
            case "MissingMaterialItem":
                result = ItemStackNetResult.MissingMaterialItem;
                return true;
            case "MissingActor":
                result = ItemStackNetResult.MissingActor;
                return true;
            case "UnknownPrimaryEffect":
                result = ItemStackNetResult.UnknownPrimaryEffect;
                return true;
            case "PrimaryEffectOutOfRange":
                result = ItemStackNetResult.PrimaryEffectOutOfRange;
                return true;
            case "PrimaryEffectUnavailable":
                result = ItemStackNetResult.PrimaryEffectUnavailable;
                return true;
            case "SecondaryEffectOutOfRange":
                result = ItemStackNetResult.SecondaryEffectOutOfRange;
                return true;
            case "SecondaryEffectUnavailable":
                result = ItemStackNetResult.SecondaryEffectUnavailable;
                return true;
            case "DstContainerEqualToCreatedOutputContainer":
                result = ItemStackNetResult.DstContainerEqualToCreatedOutputContainer;
                return true;
            case "DstContainerAndSlotEqualToSrcContainerAndSlot":
                result = ItemStackNetResult.DstContainerAndSlotEqualToSrcContainerAndSlot;
                return true;
            case "FailedToValidateSrcSlot":
                result = ItemStackNetResult.FailedToValidateSrcSlot;
                return true;
            case "FailedToValidateDstSlot":
                result = ItemStackNetResult.FailedToValidateDstSlot;
                return true;
            case "InvalidAdjustedAmount":
                result = ItemStackNetResult.InvalidAdjustedAmount;
                return true;
            case "InvalidItemSetType":
                result = ItemStackNetResult.InvalidItemSetType;
                return true;
            case "InvalidTransferAmount":
                result = ItemStackNetResult.InvalidTransferAmount;
                return true;
            case "CannotSwapItem":
                result = ItemStackNetResult.CannotSwapItem;
                return true;
            case "CannotPlaceItem":
                result = ItemStackNetResult.CannotPlaceItem;
                return true;
            case "UnhandledItemSetType":
                result = ItemStackNetResult.UnhandledItemSetType;
                return true;
            case "InvalidRemovedAmount":
                result = ItemStackNetResult.InvalidRemovedAmount;
                return true;
            case "InvalidRegion":
                result = ItemStackNetResult.InvalidRegion;
                return true;
            case "CannotDropItem":
                result = ItemStackNetResult.CannotDropItem;
                return true;
            case "CannotDestroyItem":
                result = ItemStackNetResult.CannotDestroyItem;
                return true;
            case "InvalidSourceContainer":
                result = ItemStackNetResult.InvalidSourceContainer;
                return true;
            case "ItemNotConsumed":
                result = ItemStackNetResult.ItemNotConsumed;
                return true;
            case "InvalidNumCrafts":
                result = ItemStackNetResult.InvalidNumCrafts;
                return true;
            case "InvalidCraftResultStackSize":
                result = ItemStackNetResult.InvalidCraftResultStackSize;
                return true;
            case "CannotRemoveItem":
                result = ItemStackNetResult.CannotRemoveItem;
                return true;
            case "CannotConsumeItem":
                result = ItemStackNetResult.CannotConsumeItem;
                return true;
            case "ScreenStackError":
                result = ItemStackNetResult.ScreenStackError;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
