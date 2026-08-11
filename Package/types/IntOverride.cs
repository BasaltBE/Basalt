using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class IntOverride : PlayerUpdateEntityOverridesUpdateVariant {
    public UpdateType Type = global::BedrockProtocol.Enums.UpdateType.SetIntOverride;
    public int Value;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.UpdateType constValue0 = (global::BedrockProtocol.Enums.UpdateType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.UpdateType.SetIntOverride) {
            throw new FormatException($"Expected setintoverride for Type, got {constValue0}.");
        }
        Value = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.UpdateType.SetIntOverride);
        writer.WriteInt32(Value, true);
    }
}
