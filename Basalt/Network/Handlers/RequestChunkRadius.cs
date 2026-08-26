namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Player.Traits;
using Basalt.Core.Worlds.Dimensions;

using Basalt.BedrockProtocol.Packets;

public static class RequestChunkRadius {
    public static void Handle(Server server, NetworkConnection connection, RequestChunkRadiusPacket packet) {
        int requestedRadius = packet.ChunkRadius;
        int maxViewDistance = Math.Clamp(server.Properties.MaxViewDistance, 4, ChunkViewMath.MaxBedrockViewDistance);
        int clientMax = packet.MaxChunkRadius > 0
            ? packet.MaxChunkRadius
            : ChunkViewMath.MaxBedrockViewDistance;

        int maxChebyshev = ChunkViewMath.MaxChebyshevForClientCircle(clientMax);
        int radius = Math.Clamp(requestedRadius, 4, Math.Min(maxViewDistance, maxChebyshev));
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, radius))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        int radius) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is null) {
            return;
        }

        chunkRendering.ApplyViewDistance(radius);
    }
}
