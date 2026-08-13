#nullable enable

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
        int count0 = checked((int)reader.ReadVarUInt());
        CraftResults = new List<ItemStackRequestNetworkItemInstanceDescriptor>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            ItemStackRequestNetworkItemInstanceDescriptor item0 = default!;
            ItemStackRequestNetworkItemInstanceDescriptor readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            CraftResults.Add(item0);
        }
        NumCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)CraftResults.Count));
        foreach (var item1 in CraftResults) {
            item1.Write(writer);
        }
        writer.WriteUInt8(NumCrafts);
    }
}
