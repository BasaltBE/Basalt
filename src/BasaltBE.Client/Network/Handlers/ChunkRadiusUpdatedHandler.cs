using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class ChunkRadiusUpdatedHandler(BedrockClient client) {
    internal void Handle(ChunkRadiusUpdatedPacket packet) => client.ChunkRadius = packet.ChunkRadius;
}
