using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.MoveActorAbsolute)]
public sealed record MoveActorAbsolutePacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Movement flags.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Absolute actor position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Absolute actor rotation.
    /// </summary>
    public Vec3f Rotation;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        Flags = reader.ReadUInt8();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f rotation = Rotation;
        rotation.X = ReadByteAngle(reader);
        rotation.Y = ReadByteAngle(reader);
        rotation.Z = ReadByteAngle(reader);
        Rotation = rotation;
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteUInt8(Flags);
        Position.Write(writer);
        WriteByteAngle(writer, Rotation.X);
        WriteByteAngle(writer, Rotation.Y);
        WriteByteAngle(writer, Rotation.Z);
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
