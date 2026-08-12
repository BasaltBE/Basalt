#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftRepairAndDisenchantAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant;
    public int RecipeNetId;
    public byte NumberOfRequestedCrafts;
    public int RepairCost;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant) {
            throw new FormatException($"Expected craftrepairanddisenchant for ActionType, got {constValue0}.");
        }
        RecipeNetId = reader.ReadInt32(true);
        NumberOfRequestedCrafts = reader.ReadUInt8();
        RepairCost = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftRepairAndDisenchant);
        writer.WriteInt32(RecipeNetId, true);
        writer.WriteUInt8(NumberOfRequestedCrafts);
        writer.WriteZigZag(RepairCost);
    }
}
