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
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative) {
            throw new FormatException($"Expected craftcreative for ActionType, got {constValue0}.");
        }
        CreativeItemNetId = reader.ReadVarUInt();
        NumberOfRequestedCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftCreative);
        writer.WriteVarUInt(CreativeItemNetId);
        writer.WriteUInt8(NumberOfRequestedCrafts);
    }
}
