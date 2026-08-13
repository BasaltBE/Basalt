#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftCreativeAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative;
    public uint CreativeItemNetId;
    public byte NumberOfRequestedCrafts;

    public void Read(BinaryReader reader) {
        CreativeItemNetId = reader.ReadVarUInt();
        NumberOfRequestedCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(CreativeItemNetId);
        writer.WriteUInt8(NumberOfRequestedCrafts);
    }
}
