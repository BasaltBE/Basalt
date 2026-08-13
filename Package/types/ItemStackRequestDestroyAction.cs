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
        Amount = reader.ReadUInt8();
        Source.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Amount);
        Source.Write(writer);
    }
}
