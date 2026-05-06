using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record RequestChunkRadiusPacket : DataPacket
{
    public int ChunkRadius { get; set; }
    public byte MaxChunkRadius { get; set; }

    public override PacketId PacketId => PacketId.RequestChunkRadius;

    public override void Deserialize(ref BinaryReader reader)
    {
        ChunkRadius = reader.ReadZigZag();
        MaxChunkRadius = reader.ReadUInt8();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteZigZag(ChunkRadius);
        writer.WriteUInt8(MaxChunkRadius);
    }
}
