#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemIntPayload : DataItemEntryPayloadVariant {
    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.Int;
    public int Value;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.Int) {
            throw new FormatException($"Expected int for Type, got {constValue0}.");
        }
        Value = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.Int);
        writer.WriteZigZag(Value);
    }
}
