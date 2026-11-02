using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(161)]
public sealed class CorrectPlayerMovePredictionPacket : DataPacket {
    public RewindType PredictionType;
    public Vec3 Position = new();
    public Vec3 PositionDelta = new();
    public Vec2 Rotation = new();
    public float? VehicleAngularVelocity;
    public bool OnGround;
    public ulong Tick;

    public override void Deserialize(ref BinaryReader reader) {
        PredictionType = (RewindType)reader.ReadUInt8();
        Position.Read(ref reader);
        PositionDelta.Read(ref reader);
        Rotation.Read(ref reader);
        VehicleAngularVelocity = reader.ReadBool() ? reader.ReadF32(true) : null;
        OnGround = reader.ReadBool();
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)PredictionType);
        Position.Write(ref writer);
        PositionDelta.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteBool(VehicleAngularVelocity.HasValue);
        if (VehicleAngularVelocity is float velocity)
            writer.WriteF32(velocity, true);
        writer.WriteBool(OnGround);
        writer.WriteVarULong(Tick);
    }
}
