using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(161)]
public sealed class CorrectPlayerMovePredictionPacket : DataPacket {
    public byte PredictionType;
    public Vec3 Position = new();
    public Vec3 PositionDelta = new();
    public Vec2 Rotation = new();
    public float? VehicleAngularVelocity;
    public bool OnGround;
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8(PredictionType);
        Position.Write(ref writer);
        PositionDelta.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteBool(VehicleAngularVelocity.HasValue);
        if (VehicleAngularVelocity is float velocity) writer.WriteF32(velocity, true);
        writer.WriteBool(OnGround);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        PredictionType = reader.ReadUInt8();
        Position.Read(ref reader);
        PositionDelta.Read(ref reader);
        Rotation.Read(ref reader);
        VehicleAngularVelocity = reader.ReadBool() ? reader.ReadF32(true) : null;
        OnGround = reader.ReadBool();
        Tick = reader.ReadVarULong();
    }
}
