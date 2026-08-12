#nullable enable

using System;
using BedrockProtocol.Enums;
using BedrockProtocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemCompoundTagPayload : DataItemEntryPayloadVariant {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.CompoundTag;
    public CompoundTag Value = new();

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.CompoundTag) {
            throw new FormatException($"Expected compoundtag for Type, got {constValue0}.");
        }
        Value = NBT.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.CompoundTag);
        NBT.WriteTag(writer, Value, NetworkNbtOptions);
    }
}
