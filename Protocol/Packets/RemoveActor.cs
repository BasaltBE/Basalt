using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.RemoveActor)]
public sealed record RemoveActorPacket : DataPacket
{
    /// <summary>
    /// Unique id of the actor to remove.
    /// </summary>
    public long EntityUniqueId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityUniqueId = reader.ReadVarLong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarLong(EntityUniqueId);
    }
}
