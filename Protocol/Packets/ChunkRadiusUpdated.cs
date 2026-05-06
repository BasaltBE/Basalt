using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ChunkRadiusUpdatedPacket : DataPacket
{
    public int ChunkRadius { get; set; }

    public override PacketId PacketId => PacketId.ChunkRadiusUpdated;

    public override void Deserialize(ref BinaryReader reader)
    {
        ChunkRadius = reader.ReadZigZag();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteZigZag(ChunkRadius);
    }
}
