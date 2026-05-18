using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ActorEventPacket : DataPacket
{
    public ulong ActorRuntimeId { get; set; }
    public ActorEvent Event { get; set; }
    public int Data { get; set; }

    public override PacketId PacketId => PacketId.ActorEvent;

    public override void Deserialize(BinaryReader reader)
    {
        ActorRuntimeId = reader.ReadVarULong();
        Event = (ActorEvent)reader.ReadUInt8();
        Data = reader.ReadVarInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteUInt8((byte)Event);
        writer.WriteVarInt(Data);
    }
}
