using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.NetworkStackLatency)]
public sealed record NetworkStackLatencyPacket : DataPacket {
    /// <summary>
    /// Timestamp value carried over the network.
    /// </summary>
    public long Timestamp;

    /// <summary>
    /// Whether the receiver should send a response.
    /// </summary>
    public bool NeedsResponse;

    public override void Deserialize(Binary.BinaryReader reader) {
        Timestamp = reader.ReadInt64(true);
        NeedsResponse = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteInt64(Timestamp, true);
        writer.WriteBool(NeedsResponse);
    }
}
