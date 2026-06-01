using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ActorEvent)]
public sealed record ActorEventPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong ActorRuntimeId;

    /// <summary>
    /// Actor event type.
    /// </summary>
    public ActorEvent Event;

    /// <summary>
    /// Event-specific data value.
    /// </summary>
    public int Data;
    public Optional<Vec3f> FiredAt = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ActorRuntimeId = reader.ReadVarULong();
        Event = (ActorEvent)reader.ReadUInt8();
        Data = reader.ReadZigZag();
        FiredAt.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteUInt8((byte)Event);
        writer.WriteZigZag(Data);
        FiredAt.Write(writer);
    }
}
