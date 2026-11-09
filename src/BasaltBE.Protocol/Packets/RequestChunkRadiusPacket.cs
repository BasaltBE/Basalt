using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(69)]
public sealed class RequestChunkRadiusPacket : DataPacket {
    public int ChunkRadius;
    public byte MaxChunkRadius;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag(ChunkRadius);
        writer.WriteUInt8(MaxChunkRadius);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ChunkRadius = reader.ReadZigZag();
        MaxChunkRadius = reader.ReadUInt8();
    }
}
