using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(69)]
public sealed class RequestChunkRadiusPacket : DataPacket {
    public int ChunkRadius;
    public byte MaxChunkRadius;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZong(ChunkRadius);
        writer.WriteUInt8(MaxChunkRadius);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ChunkRadius = checked((int)reader.ReadZigZong());
        MaxChunkRadius = reader.ReadUInt8();
    }
}
