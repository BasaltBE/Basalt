using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemPosPayload : DataItemEntryPayloadVariant {
    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.Pos;
    public BlockPos Value = new();

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.Pos) {
            throw new FormatException($"Expected pos for Type, got {constValue0}.");
        }
        Value.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.Pos);
        Value.Write(writer);
    }
}
