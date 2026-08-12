#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemShortPayload : DataItemEntryPayloadVariant {
    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.Short;
    public short Value;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.Short) {
            throw new FormatException($"Expected short for Type, got {constValue0}.");
        }
        Value = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.Short);
        writer.WriteInt16(Value, true);
    }
}
