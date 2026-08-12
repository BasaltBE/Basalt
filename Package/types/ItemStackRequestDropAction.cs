#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestDropAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop;
    public byte Amount;
    public ItemStackRequestSlotInfo Source = new();
    public bool Randomly;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop) {
            throw new FormatException($"Expected drop for ActionType, got {constValue0}.");
        }
        Amount = reader.ReadUInt8();
        Source.Read(reader);
        Randomly = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.Drop);
        writer.WriteUInt8(Amount);
        Source.Write(writer);
        writer.WriteBool(Randomly);
    }
}
