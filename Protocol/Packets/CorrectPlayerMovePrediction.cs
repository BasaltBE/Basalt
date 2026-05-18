using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record CorrectPlayerMovePredictionPacket : DataPacket
{
    public PredictionType PredictionType { get; set; }
    public Vec3f Position { get; set; }
    public Vec3f PositionDelta { get; set; }
    public Vec2f Rotation { get; set; }
    public OptionalValue<float> VehicleAngularVelocity { get; set; } = new();
    public bool OnGround { get; set; }
    public ulong InputTick { get; set; }

    public override PacketId PacketId => PacketId.CorrectPlayerMovePrediction;

    public override void Deserialize(BinaryReader reader)
    {
        PredictionType = (PredictionType)reader.ReadUInt8();
        Position.Read(reader);
        PositionDelta.Read(reader);
        Rotation.Read(reader);
        VehicleAngularVelocity.Read(reader, static (BinaryReader r) => r.ReadF32(true));
        OnGround = reader.ReadBool();
        InputTick = reader.ReadVarULong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteUInt8((byte)PredictionType);
        Position.Write(writer);
        PositionDelta.Write(writer);
        Rotation.Write(writer);
        VehicleAngularVelocity.Write(writer, static (BinaryWriter w, float value) => w.WriteF32(value, true));
        writer.WriteBool(OnGround);
        writer.WriteVarULong(InputTick);
    }
}
