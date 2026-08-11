using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistCommandDefinition {
    public string PresetId = string.Empty;
    public AimAssistTargetMode TargetMode;
    public Vec2 ViewAngle = new();
    public float Distance;

    public void Read(BinaryReader reader) {
        PresetId = reader.ReadVarString();
        TargetMode = (global::BedrockProtocol.Enums.AimAssistTargetMode)reader.ReadUInt8();
        ViewAngle.Read(reader);
        Distance = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PresetId);
        writer.WriteUInt8((byte)TargetMode);
        ViewAngle.Write(writer);
        writer.WriteF32(Distance, true);
    }
}
