using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ChunkRadiusUpdatedPacket : DataPacket
{
    public int ChunkRadius { get; set; }

    public override PacketId PacketId => PacketId.ChunkRadiusUpdated;

    public override void Deserialize(BinaryReader reader)
    {
        ChunkRadius = reader.ReadVarInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarInt(ChunkRadius);
    }
}
