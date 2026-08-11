using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequest {
    public int ClientRequestId;
    public List<ItemStackRequestActionVariant> Actions = [];
    public List<string> StringsToFilter = [];
    public TextProcessingEventOrigin StringsToFilterOrigin;

    public void Read(BinaryReader reader) {
        ClientRequestId = reader.ReadZigZag();
        int count2 = checked((int)reader.ReadVarUInt());
        Actions = new List<ItemStackRequestActionVariant>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            ItemStackRequestActionVariant item2 = default!;
            uint variant1002 = reader.ReadVarUInt();
            byte actionType1002 = reader.ReadUInt8();
            switch (variant1002) {
                case 0: {
                    ItemStackRequestTakeAction variantValue1002_0 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Take) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Take, got {actionType1002}.");
                    }
                    variantValue1002_0.Amount = reader.ReadUInt8();
                    variantValue1002_0.Source.Read(reader);
                    variantValue1002_0.Destination.Read(reader);
                    item2 = variantValue1002_0;
                    break;
                }
                case 1: {
                    ItemStackRequestPlaceAction variantValue1002_1 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Place) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Place, got {actionType1002}.");
                    }
                    variantValue1002_1.Amount = reader.ReadUInt8();
                    variantValue1002_1.Source.Read(reader);
                    variantValue1002_1.Destination.Read(reader);
                    item2 = variantValue1002_1;
                    break;
                }
                case 2: {
                    ItemStackRequestSwapAction variantValue1002_2 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Swap) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Swap, got {actionType1002}.");
                    }
                    variantValue1002_2.Source.Read(reader);
                    variantValue1002_2.Destination.Read(reader);
                    item2 = variantValue1002_2;
                    break;
                }
                case 3: {
                    ItemStackRequestDropAction variantValue1002_3 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop, got {actionType1002}.");
                    }
                    variantValue1002_3.Amount = reader.ReadUInt8();
                    variantValue1002_3.Source.Read(reader);
                    variantValue1002_3.Randomly = reader.ReadBool();
                    item2 = variantValue1002_3;
                    break;
                }
                case 4: {
                    ItemStackRequestDestroyAction variantValue1002_4 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy, got {actionType1002}.");
                    }
                    variantValue1002_4.Amount = reader.ReadUInt8();
                    variantValue1002_4.Source.Read(reader);
                    item2 = variantValue1002_4;
                    break;
                }
                case 5: {
                    ItemStackRequestConsumeAction variantValue1002_5 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Consume) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Consume, got {actionType1002}.");
                    }
                    variantValue1002_5.Amount = reader.ReadUInt8();
                    variantValue1002_5.Source.Read(reader);
                    item2 = variantValue1002_5;
                    break;
                }
                case 6: {
                    ItemStackRequestCreateAction variantValue1002_6 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Create) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.Create, got {actionType1002}.");
                    }
                    variantValue1002_6.ResultsIndex = reader.ReadUInt8();
                    item2 = variantValue1002_6;
                    break;
                }
                case 7: {
                    ItemStackRequestLabTableCombineAction variantValue1002_7 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenLabTableCombine) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenLabTableCombine, got {actionType1002}.");
                    }
                    item2 = variantValue1002_7;
                    break;
                }
                case 8: {
                    ItemStackRequestBeaconPaymentAction variantValue1002_8 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment, got {actionType1002}.");
                    }
                    variantValue1002_8.PrimaryEffectId = reader.ReadZigZag();
                    variantValue1002_8.SecondaryEffectId = reader.ReadZigZag();
                    item2 = variantValue1002_8;
                    break;
                }
                case 9: {
                    ItemStackRequestMineBlockAction variantValue1002_9 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenHUDMineBlock) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenHUDMineBlock, got {actionType1002}.");
                    }
                    variantValue1002_9.Slot = reader.ReadZigZag();
                    variantValue1002_9.PredictedDurability = reader.ReadZigZag();
                    variantValue1002_9.NetIdVariant = reader.ReadInt32(true);
                    item2 = variantValue1002_9;
                    break;
                }
                case 10: {
                    ItemStackRequestCraftRecipeAction variantValue1002_10 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipe) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipe, got {actionType1002}.");
                    }
                    variantValue1002_10.RecipeNetId.Read(reader);
                    variantValue1002_10.NumberOfRequestedCrafts = reader.ReadUInt8();
                    item2 = variantValue1002_10;
                    break;
                }
                case 11: {
                    ItemStackRequestCraftRecipeAutoAction variantValue1002_11 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto, got {actionType1002}.");
                    }
                    variantValue1002_11.RecipeNetId.Read(reader);
                    variantValue1002_11.NumberOfRequestedCrafts = reader.ReadUInt8();
                    int count5013 = checked((int)reader.ReadVarUInt());
                    variantValue1002_11.Ingredients = new List<RecipeIngredient>(count5013);
                    for (int i5013 = 0; i5013 < count5013; i5013++) {
                        RecipeIngredient item5013 = default!;
                        RecipeIngredient readValue6013 = new();
                        readValue6013.Read(reader);
                        item5013 = readValue6013;
                        variantValue1002_11.Ingredients.Add(item5013);
                    }
                    item2 = variantValue1002_11;
                    break;
                }
                case 12: {
                    ItemStackRequestCraftCreativeAction variantValue1002_12 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative, got {actionType1002}.");
                    }
                    variantValue1002_12.CreativeItemNetId = reader.ReadVarUInt();
                    variantValue1002_12.NumberOfRequestedCrafts = reader.ReadUInt8();
                    item2 = variantValue1002_12;
                    break;
                }
                case 13: {
                    ItemStackRequestCraftRecipeOptionalAction variantValue1002_13 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional, got {actionType1002}.");
                    }
                    variantValue1002_13.RecipeNetId.Read(reader);
                    variantValue1002_13.FilteredStringIndex = reader.ReadInt32(true);
                    item2 = variantValue1002_13;
                    break;
                }
                case 14: {
                    ItemStackRequestCraftRepairAndDisenchantAction variantValue1002_14 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant, got {actionType1002}.");
                    }
                    variantValue1002_14.RecipeNetId = reader.ReadInt32(true);
                    variantValue1002_14.NumberOfRequestedCrafts = reader.ReadUInt8();
                    variantValue1002_14.RepairCost = reader.ReadZigZag();
                    item2 = variantValue1002_14;
                    break;
                }
                case 15: {
                    ItemStackRequestCraftLoomAction variantValue1002_15 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftLoom) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftLoom, got {actionType1002}.");
                    }
                    variantValue1002_15.PatternNameId = reader.ReadVarString();
                    variantValue1002_15.NumCrafts = reader.ReadUInt8();
                    item2 = variantValue1002_15;
                    break;
                }
                case 16: {
                    ItemStackRequestCraftNonImplementedDeprecatedAction variantValue1002_16 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftNonImplemented) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftNonImplemented, got {actionType1002}.");
                    }
                    item2 = variantValue1002_16;
                    break;
                }
                case 17: {
                    ItemStackRequestCraftResultsDeprecatedAction variantValue1002_17 = new();
                    if (actionType1002 != (byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults) {
                        throw new FormatException($"Expected global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults, got {actionType1002}.");
                    }
                    int count5019 = checked((int)reader.ReadVarUInt());
                    variantValue1002_17.CraftResults = new List<ItemStackRequestNetworkItemInstanceDescriptor>(count5019);
                    for (int i5019 = 0; i5019 < count5019; i5019++) {
                        ItemStackRequestNetworkItemInstanceDescriptor item5019 = default!;
                        ItemStackRequestNetworkItemInstanceDescriptor readValue6019 = new();
                        readValue6019.Read(reader);
                        item5019 = readValue6019;
                        variantValue1002_17.CraftResults.Add(item5019);
                    }
                    variantValue1002_17.NumCrafts = reader.ReadUInt8();
                    item2 = variantValue1002_17;
                    break;
                }
                default:
                    throw new FormatException($"Unknown union variant {variant1002} for item2.");
            }
            Actions.Add(item2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        StringsToFilter = new List<string>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            string item4 = default!;
            item4 = reader.ReadVarString();
            StringsToFilter.Add(item4);
        }
        StringsToFilterOrigin = (global::BedrockProtocol.Enums.TextProcessingEventOrigin)reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(ClientRequestId);
        writer.WriteVarUInt(checked((uint)Actions.Count));
        foreach (var item3 in Actions) {
            switch (item3) {
                case ItemStackRequestTakeAction variantValue0:
                    writer.WriteVarUInt(0u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Take);
                    variantValue0.Write(writer);
                    break;
                case ItemStackRequestPlaceAction variantValue1:
                    writer.WriteVarUInt(1u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Place);
                    variantValue1.Write(writer);
                    break;
                case ItemStackRequestSwapAction variantValue2:
                    writer.WriteVarUInt(2u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Swap);
                    variantValue2.Write(writer);
                    break;
                case ItemStackRequestDropAction variantValue3:
                    writer.WriteVarUInt(3u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop);
                    variantValue3.Write(writer);
                    break;
                case ItemStackRequestDestroyAction variantValue4:
                    writer.WriteVarUInt(4u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy);
                    variantValue4.Write(writer);
                    break;
                case ItemStackRequestConsumeAction variantValue5:
                    writer.WriteVarUInt(5u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Consume);
                    variantValue5.Write(writer);
                    break;
                case ItemStackRequestCreateAction variantValue6:
                    writer.WriteVarUInt(6u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Create);
                    variantValue6.Write(writer);
                    break;
                case ItemStackRequestLabTableCombineAction variantValue7:
                    writer.WriteVarUInt(7u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenLabTableCombine);
                    variantValue7.Write(writer);
                    break;
                case ItemStackRequestBeaconPaymentAction variantValue8:
                    writer.WriteVarUInt(8u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment);
                    variantValue8.Write(writer);
                    break;
                case ItemStackRequestMineBlockAction variantValue9:
                    writer.WriteVarUInt(9u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenHUDMineBlock);
                    variantValue9.Write(writer);
                    break;
                case ItemStackRequestCraftRecipeAction variantValue10:
                    writer.WriteVarUInt(10u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipe);
                    variantValue10.Write(writer);
                    break;
                case ItemStackRequestCraftRecipeAutoAction variantValue11:
                    writer.WriteVarUInt(11u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeAuto);
                    variantValue11.Write(writer);
                    break;
                case ItemStackRequestCraftCreativeAction variantValue12:
                    writer.WriteVarUInt(12u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative);
                    variantValue12.Write(writer);
                    break;
                case ItemStackRequestCraftRecipeOptionalAction variantValue13:
                    writer.WriteVarUInt(13u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRecipeOptional);
                    variantValue13.Write(writer);
                    break;
                case ItemStackRequestCraftRepairAndDisenchantAction variantValue14:
                    writer.WriteVarUInt(14u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant);
                    variantValue14.Write(writer);
                    break;
                case ItemStackRequestCraftLoomAction variantValue15:
                    writer.WriteVarUInt(15u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftLoom);
                    variantValue15.Write(writer);
                    break;
                case ItemStackRequestCraftNonImplementedDeprecatedAction variantValue16:
                    writer.WriteVarUInt(16u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftNonImplemented);
                    variantValue16.Write(writer);
                    break;
                case ItemStackRequestCraftResultsDeprecatedAction variantValue17:
                    writer.WriteVarUInt(17u);
                    writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults);
                    variantValue17.Write(writer);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported union value for item3.");
            }
        }
        writer.WriteVarUInt(checked((uint)StringsToFilter.Count));
        foreach (var item5 in StringsToFilter) {
            writer.WriteVarString(item5);
        }
        writer.WriteInt32((int)StringsToFilterOrigin, true);
    }
}
