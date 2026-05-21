using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ChunkRadiusUpdated)]
public sealed record UpdateChunkRadiusPacket : DataPacket
{
    /// <summary>
    /// The new chunk radius that the client must use.
    /// Can not exceed their given max chunk radius
    /// </summary>
    public int ChunkRadius;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ChunkRadius = reader.ReadVarInt();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarInt(ChunkRadius);
    }
}
