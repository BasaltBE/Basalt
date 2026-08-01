using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

/// <summary>
/// @Direction Clientbound
/// Sent by the server when the client sends max distance above servers max distance,
/// pretty much a correction packet.
/// </summary>
[Packet(PacketId.ChunkRadiusUpdated)]
public sealed record UpdateChunkRadiusPacket : DataPacket {
    /// <summary>
    /// The new chunk radius that the client must use.
    /// Can not exceed their given max chunk radius
    /// </summary>
    public int ChunkRadius;

    public override void Deserialize(Binary.BinaryReader reader) {
        ChunkRadius = reader.ReadZigZag();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteZigZag(ChunkRadius);
    }
}
