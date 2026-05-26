using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetActorMotion)]
public sealed record SetActorMotionPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Velocity to apply to the actor.
    /// </summary>
    public Vec3f Velocity;

    /// <summary>
    /// Server tick when motion was sent.
    /// </summary>
    public ulong Tick;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();

        Vec3f velocity = Velocity;
        velocity.Read(reader);
        Velocity = velocity;

        Tick = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        Velocity.Write(writer);
        writer.WriteVarULong(Tick);
    }
}
