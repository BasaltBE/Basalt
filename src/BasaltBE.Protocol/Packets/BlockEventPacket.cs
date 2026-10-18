using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(26)]
public sealed class BlockEventPacket : DataPacket {
    public BlockPos Position = new();
    public BlockEventType EventType;
    public int EventData;

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        EventType = (BlockEventType)reader.ReadZigZag();
        EventData = reader.ReadZigZag();
    }

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteZigZag((int)EventType);
        writer.WriteZigZag(EventData);
    }
}
