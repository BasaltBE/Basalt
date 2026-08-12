#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemBytePayload : DataItemEntryPayloadVariant {
    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.Byte;
    public sbyte Value;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.Byte) {
            throw new FormatException($"Expected byte for Type, got {constValue0}.");
        }
        Value = reader.ReadInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.Byte);
        writer.WriteInt8(Value);
    }
}
