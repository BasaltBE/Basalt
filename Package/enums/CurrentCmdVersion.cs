#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CurrentCmdVersion {
    Invalid = -1,
    Initial = 1,
    TpRotationClamping = 2,
    NewBedrockCmdSystem = 3,
    ExecuteUsesVec3 = 4,
    CloneFixes = 5,
    UpdateAquatic = 6,
    EntitySelectorUsesVec3 = 7,
    ContainersDontDropItemsAnymore = 8,
    FiltersObeyDimensions = 9,
    ExecuteAndBlockCommandAndSelfSelectorFixes = 10,
    InstantEffectsUseTicks = 11,
    DontRegisterBrokenFunctionCommands = 12,
    ClearSpawnPointCommand = 13,
    CloneAndTeleportRotationFixes = 14,
    TeleportDimensionFixes = 15,
    CloneUpdateBlockAndTimeFixes = 16,
    CloneIntersectFix = 17,
    FunctionExecuteOrderAndChestSlotFix = 18,
    NonTickingAreasNoLongerConsideredLoaded = 19,
    SpreadplayersHazardAndResolvePlayerByNameFix = 20,
    NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged = 21,
    WaterloggingAddedToStructureCommand = 22,
    SelectorDistanceFilteredAndRelativeRotationFix = 23,
    NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix = 24,
    NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral = 25,
    ExecuteIfScoreFixes = 26,
    ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix = 27,
    ChangesToCommandOriginRotation = 28,
    RemoveAuxValueParameterFromBlockCommands = 29,
    VolumeSelectorFixes = 30,
    EnableSummonRotation = 31,
    SummonCommandDefaultRotation = 32,
    PositionalDimensionFiltering = 33,
    CommandSelectorHasItemFilterNoLongerCallsSameItemFunction = 34,
    AgentSweepingBlockTest = 34,
    BlockStateEquals = 35,
    CommandPositionFix = 35,
    CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems = 36,
    ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks = 37,
    RemoveSuicideKeyword = 38,
    CloneContainerBlockEntityRemovalFix = 39,
    StopSoundMusicFix = 40,
    SpreadPlayersStuckInGroundFixAndMaxHeightParameter = 41,
    LocateStructureOutput = 42,
    PostBlockFlattening = 43,
    TestForBlockCommandDoesNotIgnoreBlockState = 44,
    Count = 51,
    Latest = 50,
}

public static class CurrentCmdVersionExtensions {
    public static string ToProtoString(this CurrentCmdVersion value) => value.ToProtocolString();

    public static string ToProtocolString(this CurrentCmdVersion value) {
        return value switch {
            CurrentCmdVersion.Invalid => "Invalid",
            CurrentCmdVersion.Initial => "Initial",
            CurrentCmdVersion.TpRotationClamping => "TpRotationClamping",
            CurrentCmdVersion.NewBedrockCmdSystem => "NewBedrockCmdSystem",
            CurrentCmdVersion.ExecuteUsesVec3 => "ExecuteUsesVec3",
            CurrentCmdVersion.CloneFixes => "CloneFixes",
            CurrentCmdVersion.UpdateAquatic => "UpdateAquatic",
            CurrentCmdVersion.EntitySelectorUsesVec3 => "EntitySelectorUsesVec3",
            CurrentCmdVersion.ContainersDontDropItemsAnymore => "ContainersDontDropItemsAnymore",
            CurrentCmdVersion.FiltersObeyDimensions => "FiltersObeyDimensions",
            CurrentCmdVersion.ExecuteAndBlockCommandAndSelfSelectorFixes => "ExecuteAndBlockCommandAndSelfSelectorFixes",
            CurrentCmdVersion.InstantEffectsUseTicks => "InstantEffectsUseTicks",
            CurrentCmdVersion.DontRegisterBrokenFunctionCommands => "DontRegisterBrokenFunctionCommands",
            CurrentCmdVersion.ClearSpawnPointCommand => "ClearSpawnPointCommand",
            CurrentCmdVersion.CloneAndTeleportRotationFixes => "CloneAndTeleportRotationFixes",
            CurrentCmdVersion.TeleportDimensionFixes => "TeleportDimensionFixes",
            CurrentCmdVersion.CloneUpdateBlockAndTimeFixes => "CloneUpdateBlockAndTimeFixes",
            CurrentCmdVersion.CloneIntersectFix => "CloneIntersectFix",
            CurrentCmdVersion.FunctionExecuteOrderAndChestSlotFix => "FunctionExecuteOrderAndChestSlotFix",
            CurrentCmdVersion.NonTickingAreasNoLongerConsideredLoaded => "NonTickingAreasNoLongerConsideredLoaded",
            CurrentCmdVersion.SpreadplayersHazardAndResolvePlayerByNameFix => "SpreadplayersHazardAndResolvePlayerByNameFix",
            CurrentCmdVersion.NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged => "NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged",
            CurrentCmdVersion.WaterloggingAddedToStructureCommand => "WaterloggingAddedToStructureCommand",
            CurrentCmdVersion.SelectorDistanceFilteredAndRelativeRotationFix => "SelectorDistanceFilteredAndRelativeRotationFix",
            CurrentCmdVersion.NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix => "NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix",
            CurrentCmdVersion.NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral => "NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral",
            CurrentCmdVersion.ExecuteIfScoreFixes => "ExecuteIfScoreFixes",
            CurrentCmdVersion.ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix => "ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix",
            CurrentCmdVersion.ChangesToCommandOriginRotation => "ChangesToCommandOriginRotation",
            CurrentCmdVersion.RemoveAuxValueParameterFromBlockCommands => "RemoveAuxValueParameterFromBlockCommands",
            CurrentCmdVersion.VolumeSelectorFixes => "VolumeSelectorFixes",
            CurrentCmdVersion.EnableSummonRotation => "EnableSummonRotation",
            CurrentCmdVersion.SummonCommandDefaultRotation => "SummonCommandDefaultRotation",
            CurrentCmdVersion.PositionalDimensionFiltering => "PositionalDimensionFiltering",
            CurrentCmdVersion.CommandSelectorHasItemFilterNoLongerCallsSameItemFunction => "CommandSelectorHasItemFilterNoLongerCallsSameItemFunction",
            CurrentCmdVersion.BlockStateEquals => "BlockStateEquals",
            CurrentCmdVersion.CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems => "CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems",
            CurrentCmdVersion.ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks => "ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks",
            CurrentCmdVersion.RemoveSuicideKeyword => "RemoveSuicideKeyword",
            CurrentCmdVersion.CloneContainerBlockEntityRemovalFix => "CloneContainerBlockEntityRemovalFix",
            CurrentCmdVersion.StopSoundMusicFix => "StopSoundMusicFix",
            CurrentCmdVersion.SpreadPlayersStuckInGroundFixAndMaxHeightParameter => "SpreadPlayersStuckInGroundFixAndMaxHeightParameter",
            CurrentCmdVersion.LocateStructureOutput => "LocateStructureOutput",
            CurrentCmdVersion.PostBlockFlattening => "PostBlockFlattening",
            CurrentCmdVersion.TestForBlockCommandDoesNotIgnoreBlockState => "TestForBlockCommandDoesNotIgnoreBlockState",
            CurrentCmdVersion.Count => "Count",
            CurrentCmdVersion.Latest => "Latest",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CurrentCmdVersion value.")
        };
    }

    public static CurrentCmdVersion FromProtocolString(string value) {
        return value switch {
            "Invalid" => CurrentCmdVersion.Invalid,
            "Initial" => CurrentCmdVersion.Initial,
            "TpRotationClamping" => CurrentCmdVersion.TpRotationClamping,
            "NewBedrockCmdSystem" => CurrentCmdVersion.NewBedrockCmdSystem,
            "ExecuteUsesVec3" => CurrentCmdVersion.ExecuteUsesVec3,
            "CloneFixes" => CurrentCmdVersion.CloneFixes,
            "UpdateAquatic" => CurrentCmdVersion.UpdateAquatic,
            "EntitySelectorUsesVec3" => CurrentCmdVersion.EntitySelectorUsesVec3,
            "ContainersDontDropItemsAnymore" => CurrentCmdVersion.ContainersDontDropItemsAnymore,
            "FiltersObeyDimensions" => CurrentCmdVersion.FiltersObeyDimensions,
            "ExecuteAndBlockCommandAndSelfSelectorFixes" => CurrentCmdVersion.ExecuteAndBlockCommandAndSelfSelectorFixes,
            "InstantEffectsUseTicks" => CurrentCmdVersion.InstantEffectsUseTicks,
            "DontRegisterBrokenFunctionCommands" => CurrentCmdVersion.DontRegisterBrokenFunctionCommands,
            "ClearSpawnPointCommand" => CurrentCmdVersion.ClearSpawnPointCommand,
            "CloneAndTeleportRotationFixes" => CurrentCmdVersion.CloneAndTeleportRotationFixes,
            "TeleportDimensionFixes" => CurrentCmdVersion.TeleportDimensionFixes,
            "CloneUpdateBlockAndTimeFixes" => CurrentCmdVersion.CloneUpdateBlockAndTimeFixes,
            "CloneIntersectFix" => CurrentCmdVersion.CloneIntersectFix,
            "FunctionExecuteOrderAndChestSlotFix" => CurrentCmdVersion.FunctionExecuteOrderAndChestSlotFix,
            "NonTickingAreasNoLongerConsideredLoaded" => CurrentCmdVersion.NonTickingAreasNoLongerConsideredLoaded,
            "SpreadplayersHazardAndResolvePlayerByNameFix" => CurrentCmdVersion.SpreadplayersHazardAndResolvePlayerByNameFix,
            "NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged" => CurrentCmdVersion.NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged,
            "WaterloggingAddedToStructureCommand" => CurrentCmdVersion.WaterloggingAddedToStructureCommand,
            "SelectorDistanceFilteredAndRelativeRotationFix" => CurrentCmdVersion.SelectorDistanceFilteredAndRelativeRotationFix,
            "NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix" => CurrentCmdVersion.NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix,
            "NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral" => CurrentCmdVersion.NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral,
            "ExecuteIfScoreFixes" => CurrentCmdVersion.ExecuteIfScoreFixes,
            "ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix" => CurrentCmdVersion.ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix,
            "ChangesToCommandOriginRotation" => CurrentCmdVersion.ChangesToCommandOriginRotation,
            "RemoveAuxValueParameterFromBlockCommands" => CurrentCmdVersion.RemoveAuxValueParameterFromBlockCommands,
            "VolumeSelectorFixes" => CurrentCmdVersion.VolumeSelectorFixes,
            "EnableSummonRotation" => CurrentCmdVersion.EnableSummonRotation,
            "SummonCommandDefaultRotation" => CurrentCmdVersion.SummonCommandDefaultRotation,
            "PositionalDimensionFiltering" => CurrentCmdVersion.PositionalDimensionFiltering,
            "CommandSelectorHasItemFilterNoLongerCallsSameItemFunction" => CurrentCmdVersion.CommandSelectorHasItemFilterNoLongerCallsSameItemFunction,
            "AgentSweepingBlockTest" => CurrentCmdVersion.AgentSweepingBlockTest,
            "BlockStateEquals" => CurrentCmdVersion.BlockStateEquals,
            "CommandPositionFix" => CurrentCmdVersion.CommandPositionFix,
            "CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems" => CurrentCmdVersion.CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems,
            "ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks" => CurrentCmdVersion.ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks,
            "RemoveSuicideKeyword" => CurrentCmdVersion.RemoveSuicideKeyword,
            "CloneContainerBlockEntityRemovalFix" => CurrentCmdVersion.CloneContainerBlockEntityRemovalFix,
            "StopSoundMusicFix" => CurrentCmdVersion.StopSoundMusicFix,
            "SpreadPlayersStuckInGroundFixAndMaxHeightParameter" => CurrentCmdVersion.SpreadPlayersStuckInGroundFixAndMaxHeightParameter,
            "LocateStructureOutput" => CurrentCmdVersion.LocateStructureOutput,
            "PostBlockFlattening" => CurrentCmdVersion.PostBlockFlattening,
            "TestForBlockCommandDoesNotIgnoreBlockState" => CurrentCmdVersion.TestForBlockCommandDoesNotIgnoreBlockState,
            "Count" => CurrentCmdVersion.Count,
            "Latest" => CurrentCmdVersion.Latest,
            _ => throw new ArgumentException($"Unknown CurrentCmdVersion protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CurrentCmdVersion result) {
        switch (value) {
            case "Invalid":
                result = CurrentCmdVersion.Invalid;
                return true;
            case "Initial":
                result = CurrentCmdVersion.Initial;
                return true;
            case "TpRotationClamping":
                result = CurrentCmdVersion.TpRotationClamping;
                return true;
            case "NewBedrockCmdSystem":
                result = CurrentCmdVersion.NewBedrockCmdSystem;
                return true;
            case "ExecuteUsesVec3":
                result = CurrentCmdVersion.ExecuteUsesVec3;
                return true;
            case "CloneFixes":
                result = CurrentCmdVersion.CloneFixes;
                return true;
            case "UpdateAquatic":
                result = CurrentCmdVersion.UpdateAquatic;
                return true;
            case "EntitySelectorUsesVec3":
                result = CurrentCmdVersion.EntitySelectorUsesVec3;
                return true;
            case "ContainersDontDropItemsAnymore":
                result = CurrentCmdVersion.ContainersDontDropItemsAnymore;
                return true;
            case "FiltersObeyDimensions":
                result = CurrentCmdVersion.FiltersObeyDimensions;
                return true;
            case "ExecuteAndBlockCommandAndSelfSelectorFixes":
                result = CurrentCmdVersion.ExecuteAndBlockCommandAndSelfSelectorFixes;
                return true;
            case "InstantEffectsUseTicks":
                result = CurrentCmdVersion.InstantEffectsUseTicks;
                return true;
            case "DontRegisterBrokenFunctionCommands":
                result = CurrentCmdVersion.DontRegisterBrokenFunctionCommands;
                return true;
            case "ClearSpawnPointCommand":
                result = CurrentCmdVersion.ClearSpawnPointCommand;
                return true;
            case "CloneAndTeleportRotationFixes":
                result = CurrentCmdVersion.CloneAndTeleportRotationFixes;
                return true;
            case "TeleportDimensionFixes":
                result = CurrentCmdVersion.TeleportDimensionFixes;
                return true;
            case "CloneUpdateBlockAndTimeFixes":
                result = CurrentCmdVersion.CloneUpdateBlockAndTimeFixes;
                return true;
            case "CloneIntersectFix":
                result = CurrentCmdVersion.CloneIntersectFix;
                return true;
            case "FunctionExecuteOrderAndChestSlotFix":
                result = CurrentCmdVersion.FunctionExecuteOrderAndChestSlotFix;
                return true;
            case "NonTickingAreasNoLongerConsideredLoaded":
                result = CurrentCmdVersion.NonTickingAreasNoLongerConsideredLoaded;
                return true;
            case "SpreadplayersHazardAndResolvePlayerByNameFix":
                result = CurrentCmdVersion.SpreadplayersHazardAndResolvePlayerByNameFix;
                return true;
            case "NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged":
                result = CurrentCmdVersion.NewExecuteCommandSyntaxExperimentAndChestLootTableFixAndTeleportFacingVerticalUnclampedAndLocateBiomeAndFeatureMerged;
                return true;
            case "WaterloggingAddedToStructureCommand":
                result = CurrentCmdVersion.WaterloggingAddedToStructureCommand;
                return true;
            case "SelectorDistanceFilteredAndRelativeRotationFix":
                result = CurrentCmdVersion.SelectorDistanceFilteredAndRelativeRotationFix;
                return true;
            case "NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix":
                result = CurrentCmdVersion.NewSummonCommandAddedRotationOptionsAndBubbleColumnCloneFixAndExecuteInDimensionTeleportFixAndNewExecuteRotationFix;
                return true;
            case "NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral":
                result = CurrentCmdVersion.NewExecuteCommandReleaseEnchantCommandLevelFixAndHasItemDataFixAndCommandDeferral;
                return true;
            case "ExecuteIfScoreFixes":
                result = CurrentCmdVersion.ExecuteIfScoreFixes;
                return true;
            case "ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix":
                result = CurrentCmdVersion.ReplaceItemAndLootReplaceBlockCommandsDoNotPlaceItemsIntoCauldronsFix;
                return true;
            case "ChangesToCommandOriginRotation":
                result = CurrentCmdVersion.ChangesToCommandOriginRotation;
                return true;
            case "RemoveAuxValueParameterFromBlockCommands":
                result = CurrentCmdVersion.RemoveAuxValueParameterFromBlockCommands;
                return true;
            case "VolumeSelectorFixes":
                result = CurrentCmdVersion.VolumeSelectorFixes;
                return true;
            case "EnableSummonRotation":
                result = CurrentCmdVersion.EnableSummonRotation;
                return true;
            case "SummonCommandDefaultRotation":
                result = CurrentCmdVersion.SummonCommandDefaultRotation;
                return true;
            case "PositionalDimensionFiltering":
                result = CurrentCmdVersion.PositionalDimensionFiltering;
                return true;
            case "CommandSelectorHasItemFilterNoLongerCallsSameItemFunction":
                result = CurrentCmdVersion.CommandSelectorHasItemFilterNoLongerCallsSameItemFunction;
                return true;
            case "AgentSweepingBlockTest":
                result = CurrentCmdVersion.AgentSweepingBlockTest;
                return true;
            case "BlockStateEquals":
                result = CurrentCmdVersion.BlockStateEquals;
                return true;
            case "CommandPositionFix":
                result = CurrentCmdVersion.CommandPositionFix;
                return true;
            case "CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems":
                result = CurrentCmdVersion.CommandSelectorHasItemFilterUsesDataAsDamageForSelectingDamageableItems;
                return true;
            case "ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks":
                result = CurrentCmdVersion.ExecuteDetectConditionSubcommandNotAllowNonLoadedBlocks;
                return true;
            case "RemoveSuicideKeyword":
                result = CurrentCmdVersion.RemoveSuicideKeyword;
                return true;
            case "CloneContainerBlockEntityRemovalFix":
                result = CurrentCmdVersion.CloneContainerBlockEntityRemovalFix;
                return true;
            case "StopSoundMusicFix":
                result = CurrentCmdVersion.StopSoundMusicFix;
                return true;
            case "SpreadPlayersStuckInGroundFixAndMaxHeightParameter":
                result = CurrentCmdVersion.SpreadPlayersStuckInGroundFixAndMaxHeightParameter;
                return true;
            case "LocateStructureOutput":
                result = CurrentCmdVersion.LocateStructureOutput;
                return true;
            case "PostBlockFlattening":
                result = CurrentCmdVersion.PostBlockFlattening;
                return true;
            case "TestForBlockCommandDoesNotIgnoreBlockState":
                result = CurrentCmdVersion.TestForBlockCommandDoesNotIgnoreBlockState;
                return true;
            case "Count":
                result = CurrentCmdVersion.Count;
                return true;
            case "Latest":
                result = CurrentCmdVersion.Latest;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
