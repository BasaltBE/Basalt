using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class MoveActorDeltaData : DataType {
    public ulong ActorRuntimeId;
    public float? PositionX;
    public float? PositionY;
    public float? PositionZ;
    public sbyte? RotationX;
    public sbyte? RotationY;
    public sbyte? RotationYHead;
    public bool IsOnGround;
    public bool ForceMove;
    public bool ForceMoveLocalEntity;
    public bool ForceCompletion;
    public ulong Ticks;

    public override void Read(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        PositionX = reader.ReadBool() ? reader.ReadF32(true) : null;
        PositionY = reader.ReadBool() ? reader.ReadF32(true) : null;
        PositionZ = reader.ReadBool() ? reader.ReadF32(true) : null;
        RotationX = reader.ReadBool() ? (sbyte)reader.ReadUInt8() : null;
        RotationY = reader.ReadBool() ? (sbyte)reader.ReadUInt8() : null;
        RotationYHead = reader.ReadBool() ? (sbyte)reader.ReadUInt8() : null;
        IsOnGround = reader.ReadBool();
        ForceMove = reader.ReadBool();
        ForceMoveLocalEntity = reader.ReadBool();
        ForceCompletion = reader.ReadBool();
        Ticks = reader.ReadVarULong();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteBool(PositionX.HasValue);
        if (PositionX is float positionX) writer.WriteF32(positionX, true);
        writer.WriteBool(PositionY.HasValue);
        if (PositionY is float positionY) writer.WriteF32(positionY, true);
        writer.WriteBool(PositionZ.HasValue);
        if (PositionZ is float positionZ) writer.WriteF32(positionZ, true);
        writer.WriteBool(RotationX.HasValue);
        if (RotationX is sbyte rotationX) writer.WriteUInt8((byte)rotationX);
        writer.WriteBool(RotationY.HasValue);
        if (RotationY is sbyte rotationY) writer.WriteUInt8((byte)rotationY);
        writer.WriteBool(RotationYHead.HasValue);
        if (RotationYHead is sbyte rotationYHead) writer.WriteUInt8((byte)rotationYHead);
        writer.WriteBool(IsOnGround);
        writer.WriteBool(ForceMove);
        writer.WriteBool(ForceMoveLocalEntity);
        writer.WriteBool(ForceCompletion);
        writer.WriteVarULong(Ticks);
    }
}
