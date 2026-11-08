using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class RequestChunkRadiusHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, RequestChunkRadiusPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.RequestChunkRadius");
        if (player is null) throw new InvalidDataException("Chunk radius request arrived before player login.");

        int clientMaxCircle = packet.MaxChunkRadius > 0
            ? packet.MaxChunkRadius
            : ChunkViewMath.MaxBedrockViewDistance;
        int maxChebyshevRadius = Math.Min(
            network.Server.Properties.MaxViewDistance,
            ChunkViewMath.MaxChebyshevForClientCircle(clientMaxCircle)
        );
        player.ChunkRadius = Math.Clamp(packet.ChunkRadius, 0, maxChebyshevRadius);
        player.SendPacket(new ChunkRadiusUpdatedPacket {
            ChunkRadius = ChunkViewMath.SquareToCircle(player.ChunkRadius)
        });
    }
}
