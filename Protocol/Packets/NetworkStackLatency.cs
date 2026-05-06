using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record NetworkStackLatencyPacket : DataPacket
{
    public long Timestamp { get; set; }
    public bool NeedsResponse { get; set; }

    public override PacketId PacketId => PacketId.NetworkStackLatency;

    public override void Deserialize(ref BinaryReader reader)
    {
        Timestamp = reader.ReadInt64(true);
        NeedsResponse = reader.ReadBool();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteInt64(Timestamp, true);
        writer.WriteBool(NeedsResponse);
    }
}
