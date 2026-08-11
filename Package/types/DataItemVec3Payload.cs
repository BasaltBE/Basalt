using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemVec3Payload : DataItemEntryPayloadVariant {
    public DataItemType Type = global::BedrockProtocol.Enums.DataItemType.Vec3;
    public Vec3 Value = new();

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.DataItemType constValue0 = (global::BedrockProtocol.Enums.DataItemType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.DataItemType.Vec3) {
            throw new FormatException($"Expected vec3 for Type, got {constValue0}.");
        }
        Value.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.DataItemType.Vec3);
        Value.Write(writer);
    }
}
