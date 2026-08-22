using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(26)]
public sealed class BlockEventPacket : DataPacket {
    public BlockPos Position = new();
    public int EventType;
    public int EventValue;

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteZigZag(EventType);
        writer.WriteZigZag(EventValue);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        EventType = reader.ReadZigZag();
        EventValue = reader.ReadZigZag();
    }
}
