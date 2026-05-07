using Basalt.Core;
using Basalt.Entity.Traits.PlayerTraits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class RequestChunkRadius
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestChunkRadiusPacket packet = new();
        packet.Deserialize(packetBuffer);

        int requestedRadius = Math.Max(packet.ChunkRadius, packet.MaxChunkRadius);
        int radius = Math.Clamp(requestedRadius, 4, 16);
        ChunkRadiusUpdatedPacket response = new()
        {
            ChunkRadius = radius
        };
        server.Network.SendPacket(connection, response);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is null)
        {
            return;
        }

        chunkRendering.ApplyViewDistance(radius);
        Logger.Info($"[{player.Username}] Requested chunk radius: {packet.ChunkRadius} (Max: {packet.MaxChunkRadius}), accepted: {radius}");
    }
}
