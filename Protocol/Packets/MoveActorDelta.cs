using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.MoveActorDelta)]
public sealed record MoveActorDeltaPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Movement delta flags.
    /// </summary>
    public ushort Flags;

    /// <summary>
    /// Position payload values.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Rotation payload values.
    /// </summary>
    public Vec3f Rotation;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        Flags = reader.ReadUInt16(true);

        Vec3f position = Position;
        position.X = (Flags & 1) != 0 ? reader.ReadF32(true) : 0;
        position.Y = (Flags & 2) != 0 ? reader.ReadF32(true) : 0;
        position.Z = (Flags & 4) != 0 ? reader.ReadF32(true) : 0;
        Position = position;

        Vec3f rotation = Rotation;
        rotation.X = (Flags & 8) != 0 ? ReadByteAngle(reader) : 0;
        rotation.Y = (Flags & 16) != 0 ? ReadByteAngle(reader) : 0;
        rotation.Z = (Flags & 32) != 0 ? ReadByteAngle(reader) : 0;
        Rotation = rotation;
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteUInt16(Flags, true);

        if ((Flags & 1) != 0) writer.WriteF32(Position.X, true);
        if ((Flags & 2) != 0) writer.WriteF32(Position.Y, true);
        if ((Flags & 4) != 0) writer.WriteF32(Position.Z, true);
        if ((Flags & 8) != 0) WriteByteAngle(writer, Rotation.X);
        if ((Flags & 16) != 0) WriteByteAngle(writer, Rotation.Y);
        if ((Flags & 32) != 0) WriteByteAngle(writer, Rotation.Z);
    }

    private static float ReadByteAngle(Binary.BinaryReader reader)
    {
        return reader.ReadUInt8() * (360f / 256f);
    }

    private static void WriteByteAngle(Binary.BinaryWriter writer, float value)
    {
        writer.WriteUInt8((byte)(value * (256f / 360f)));
    }
}
