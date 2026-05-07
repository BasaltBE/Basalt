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

    public override void Deserialize(ref BinaryReader reader)
    {
        PredictionType = (PredictionType)reader.ReadUInt8();
        Position.Read(ref reader);
        PositionDelta.Read(ref reader);
        Rotation.Read(ref reader);
        VehicleAngularVelocity.Read(ref reader, static (ref BinaryReader r) => r.ReadF32(true));
        OnGround = reader.ReadBool();
        InputTick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt8((byte)PredictionType);
        Position.Write(ref writer);
        PositionDelta.Write(ref writer);
        Rotation.Write(ref writer);
        VehicleAngularVelocity.Write(ref writer, static (ref BinaryWriter w, float value) => w.WriteF32(value, true));
        writer.WriteBool(OnGround);
        writer.WriteVarULong(InputTick);
    }
}
