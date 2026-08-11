using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftResultsDeprecatedAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults;
    public List<ItemStackRequestNetworkItemInstanceDescriptor> CraftResults = [];
    public byte NumCrafts;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults) {
            throw new FormatException($"Expected craftresults for ActionType, got {constValue0}.");
        }
        int count2 = checked((int)reader.ReadVarUInt());
        CraftResults = new List<ItemStackRequestNetworkItemInstanceDescriptor>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            ItemStackRequestNetworkItemInstanceDescriptor item2 = default!;
            ItemStackRequestNetworkItemInstanceDescriptor readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            CraftResults.Add(item2);
        }
        NumCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftResults);
        writer.WriteVarUInt(checked((uint)CraftResults.Count));
        foreach (var item3 in CraftResults) {
            item3.Write(writer);
        }
        writer.WriteUInt8(NumCrafts);
    }
}
