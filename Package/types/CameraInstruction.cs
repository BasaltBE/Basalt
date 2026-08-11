using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraInstruction {
    public SetInstruction Set = new();
    public bool Clear;
    public FadeInstruction Fade = new();
    public TargetInstruction Target = new();
    public bool RemoveTarget;
    public FovInstruction FieldOfView = new();
    public SplineInstruction Spline = new();
    public AttachToEntityInstruction AttachToEntity = new();
    public bool DetachFromEntity;

    public void Read(BinaryReader reader) {
        Set.Read(reader);
        Clear = reader.ReadBool();
        Fade.Read(reader);
        Target.Read(reader);
        RemoveTarget = reader.ReadBool();
        FieldOfView.Read(reader);
        Spline.Read(reader);
        AttachToEntity.Read(reader);
        DetachFromEntity = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        Set.Write(writer);
        writer.WriteBool(Clear);
        Fade.Write(writer);
        Target.Write(writer);
        writer.WriteBool(RemoveTarget);
        FieldOfView.Write(writer);
        Spline.Write(writer);
        AttachToEntity.Write(writer);
        writer.WriteBool(DetachFromEntity);
    }
}
