using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MoveActorDeltaData {
    public ActorRuntimeID ActorRuntimeID = new();
    public float? NewPositionX;
    public float? NewPositionY;
    public float? NewPositionZ;
    public sbyte? RotationX;
    public sbyte? RotationY;
    public sbyte? RotationYHead;
    public bool IsOnGround;
    public bool ForceMove;
    public bool ForceMoveLocalEntity;
    public bool ForceCompletion;

    public void Read(BinaryReader reader) {
        ActorRuntimeID.Read(reader);
        if (reader.ReadBool()) {
            NewPositionX = reader.ReadF32(true);
        } else {
            NewPositionX = default;
        }
        if (reader.ReadBool()) {
            NewPositionY = reader.ReadF32(true);
        } else {
            NewPositionY = default;
        }
        if (reader.ReadBool()) {
            NewPositionZ = reader.ReadF32(true);
        } else {
            NewPositionZ = default;
        }
        if (reader.ReadBool()) {
            RotationX = reader.ReadInt8();
        } else {
            RotationX = default;
        }
        if (reader.ReadBool()) {
            RotationY = reader.ReadInt8();
        } else {
            RotationY = default;
        }
        if (reader.ReadBool()) {
            RotationYHead = reader.ReadInt8();
        } else {
            RotationYHead = default;
        }
        IsOnGround = reader.ReadBool();
        ForceMove = reader.ReadBool();
        ForceMoveLocalEntity = reader.ReadBool();
        ForceCompletion = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        ActorRuntimeID.Write(writer);
        writer.WriteBool(NewPositionX is not null);
        if (NewPositionX is { } optionalValue3) {
            writer.WriteF32(optionalValue3, true);
        }
        writer.WriteBool(NewPositionY is not null);
        if (NewPositionY is { } optionalValue5) {
            writer.WriteF32(optionalValue5, true);
        }
        writer.WriteBool(NewPositionZ is not null);
        if (NewPositionZ is { } optionalValue7) {
            writer.WriteF32(optionalValue7, true);
        }
        writer.WriteBool(RotationX is not null);
        if (RotationX is { } optionalValue9) {
            writer.WriteInt8(optionalValue9);
        }
        writer.WriteBool(RotationY is not null);
        if (RotationY is { } optionalValue11) {
            writer.WriteInt8(optionalValue11);
        }
        writer.WriteBool(RotationYHead is not null);
        if (RotationYHead is { } optionalValue13) {
            writer.WriteInt8(optionalValue13);
        }
        writer.WriteBool(IsOnGround);
        writer.WriteBool(ForceMove);
        writer.WriteBool(ForceMoveLocalEntity);
        writer.WriteBool(ForceCompletion);
    }
}
