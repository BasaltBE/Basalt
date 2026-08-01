using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.RequestChunkRadius)]
public sealed record RequestChunkRadiusPacket : DataPacket {
    /// <summary>
    /// The chunk radius to request
    /// </summary>
    public int ChunkRadius;
    /// <summary>
    /// The maximum chunk radius that is reasonable
    /// </summary>
    public byte MaxChunkRadius;

    public override void Deserialize(Binary.BinaryReader reader) {
        ChunkRadius = reader.ReadZigZag();
        MaxChunkRadius = reader.ReadUInt8();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteZigZag(ChunkRadius);
        writer.WriteUInt8(MaxChunkRadius);
    }
}
