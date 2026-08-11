using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FloatOverride : PlayerUpdateEntityOverridesUpdateVariant {
    public UpdateType Type = global::BedrockProtocol.Enums.UpdateType.SetFloatOverride;
    public float Value;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.UpdateType constValue0 = (global::BedrockProtocol.Enums.UpdateType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.UpdateType.SetFloatOverride) {
            throw new FormatException($"Expected setfloatoverride for Type, got {constValue0}.");
        }
        Value = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.UpdateType.SetFloatOverride);
        writer.WriteF32(Value, true);
    }
}
