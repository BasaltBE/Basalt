#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestDestroyAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy;
    public byte Amount;
    public ItemStackRequestSlotInfo Source = new();

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy) {
            throw new FormatException($"Expected destroy for ActionType, got {constValue0}.");
        }
        Amount = reader.ReadUInt8();
        Source.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Destroy);
        writer.WriteUInt8(Amount);
        Source.Write(writer);
    }
}
