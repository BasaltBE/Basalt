using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.CorrectPlayerMovePrediction)]
public sealed record CorrectPlayerMovePredictionPacket : DataPacket {
    /// <summary>
    /// Prediction correction type.
    /// </summary>
    public PredictionType PredictionType;

    /// <summary>
    /// Corrected world position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Position delta from prediction.
    /// </summary>
    public Vec3f PositionDelta;

    /// <summary>
    /// Corrected rotation values.
    /// </summary>
    public Vec2f Rotation;

    /// <summary>
    /// Optional vehicle angular velocity.
    /// </summary>
    public OptionalValue<float> VehicleAngularVelocity = new();

    /// <summary>
    /// Whether the actor is on ground.
    /// </summary>
    public bool OnGround;

    /// <summary>
    /// Input tick this correction targets.
    /// </summary>
    public ulong InputTick;

    public override void Deserialize(Binary.BinaryReader reader) {
        PredictionType = (PredictionType)reader.ReadUInt8();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f positionDelta = PositionDelta;
        positionDelta.Read(reader);
        PositionDelta = positionDelta;

        Vec2f rotation = Rotation;
        rotation.Read(reader);
        Rotation = rotation;

        VehicleAngularVelocity.Read(reader, static (Binary.BinaryReader r) => r.ReadF32(true));
        OnGround = reader.ReadBool();
        InputTick = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteUInt8((byte)PredictionType);
        Position.Write(writer);
        PositionDelta.Write(writer);
        Rotation.Write(writer);
        VehicleAngularVelocity.Write(writer, static (Binary.BinaryWriter w, float value) => w.WriteF32(value, true));
        writer.WriteBool(OnGround);
        writer.WriteVarULong(InputTick);
    }
}
