using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(111)]
public sealed class MoveActorDeltaPacket : DataPacket {
    public ulong ActorRuntimeId;
    public float? PositionX;
    public float? PositionY;
    public float? PositionZ;
    public sbyte? RotationX;
    public sbyte? RotationY;
    public sbyte? RotationYHead;
    public bool OnGround;
    public bool ForceMove;
    public bool ForceMoveLocalEntity;
    public bool ForceCompletion;
    public ulong Ticks;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        WriteOptional(ref writer, PositionX, static (ref BinaryWriter w, float value) => w.WriteF32(value, true));
        WriteOptional(ref writer, PositionY, static (ref BinaryWriter w, float value) => w.WriteF32(value, true));
        WriteOptional(ref writer, PositionZ, static (ref BinaryWriter w, float value) => w.WriteF32(value, true));
        WriteOptional(ref writer, RotationX, static (ref BinaryWriter w, sbyte value) => w.WriteInt8(value));
        WriteOptional(ref writer, RotationY, static (ref BinaryWriter w, sbyte value) => w.WriteInt8(value));
        WriteOptional(ref writer, RotationYHead, static (ref BinaryWriter w, sbyte value) => w.WriteInt8(value));
        writer.WriteBool(OnGround);
        writer.WriteBool(ForceMove);
        writer.WriteBool(ForceMoveLocalEntity);
        writer.WriteBool(ForceCompletion);
        writer.WriteVarULong(Ticks);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        PositionX = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadF32(true));
        PositionY = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadF32(true));
        PositionZ = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadF32(true));
        RotationX = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadInt8());
        RotationY = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadInt8());
        RotationYHead = ReadOptional(ref reader, static (ref BinaryReader r) => r.ReadInt8());
        OnGround = reader.ReadBool();
        ForceMove = reader.ReadBool();
        ForceMoveLocalEntity = reader.ReadBool();
        ForceCompletion = reader.ReadBool();
        Ticks = reader.ReadVarULong();
    }

    static void WriteOptional<T>(ref BinaryWriter writer, T? value, WriteValue<T> write) where T : struct {
        writer.WriteBool(value.HasValue);
        if (value is T actual) write(ref writer, actual);
    }

    static T? ReadOptional<T>(ref BinaryReader reader, ReadValue<T> read) where T : struct => reader.ReadBool() ? read(ref reader) : null;
    delegate void WriteValue<T>(ref BinaryWriter writer, T value);
    delegate T ReadValue<T>(ref BinaryReader reader);
}
