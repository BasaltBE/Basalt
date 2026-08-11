using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraPresets {
    public string Name = string.Empty;
    public string InheritFrom = string.Empty;
    public float PosX;
    public float PosY;
    public float PosZ;
    public float RotX;
    public float RotY;
    public float RotationSpeed;
    public bool SnapToTarget;
    public Vec2 HorizontalRotationLimit = new();
    public Vec2 VerticalRotationLimit = new();
    public bool ContinueTargeting;
    public float BlockListeningRadius;
    public Vec2 ViewOffset = new();
    public Vec3 EntityOffset = new();
    public float Radius;
    public float YawLimitMin;
    public float YawLimitMax;
    public AudioListener Listener;
    public bool PlayerEffects;
    public CameraAimAssistCommandDefinition AimAssist = new();
    public ControlScheme ControlScheme;

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        InheritFrom = reader.ReadVarString();
        PosX = reader.ReadF32(true);
        PosY = reader.ReadF32(true);
        PosZ = reader.ReadF32(true);
        RotX = reader.ReadF32(true);
        RotY = reader.ReadF32(true);
        RotationSpeed = reader.ReadF32(true);
        SnapToTarget = reader.ReadBool();
        HorizontalRotationLimit.Read(reader);
        VerticalRotationLimit.Read(reader);
        ContinueTargeting = reader.ReadBool();
        BlockListeningRadius = reader.ReadF32(true);
        ViewOffset.Read(reader);
        EntityOffset.Read(reader);
        Radius = reader.ReadF32(true);
        YawLimitMin = reader.ReadF32(true);
        YawLimitMax = reader.ReadF32(true);
        Listener = (global::BedrockProtocol.Enums.AudioListener)reader.ReadUInt8();
        PlayerEffects = reader.ReadBool();
        AimAssist.Read(reader);
        ControlScheme = (global::BedrockProtocol.Enums.ControlScheme)reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarString(InheritFrom);
        writer.WriteF32(PosX, true);
        writer.WriteF32(PosY, true);
        writer.WriteF32(PosZ, true);
        writer.WriteF32(RotX, true);
        writer.WriteF32(RotY, true);
        writer.WriteF32(RotationSpeed, true);
        writer.WriteBool(SnapToTarget);
        HorizontalRotationLimit.Write(writer);
        VerticalRotationLimit.Write(writer);
        writer.WriteBool(ContinueTargeting);
        writer.WriteF32(BlockListeningRadius, true);
        ViewOffset.Write(writer);
        EntityOffset.Write(writer);
        writer.WriteF32(Radius, true);
        writer.WriteF32(YawLimitMin, true);
        writer.WriteF32(YawLimitMax, true);
        writer.WriteUInt8((byte)Listener);
        writer.WriteBool(PlayerEffects);
        AimAssist.Write(writer);
        writer.WriteUInt8((byte)ControlScheme);
    }
}
