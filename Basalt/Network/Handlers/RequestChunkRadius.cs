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
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet.Deserialize(reader);

        int requestedRadius = packet.MaxChunkRadius > 0
            ? Math.Min(packet.ChunkRadius, packet.MaxChunkRadius)
            : packet.ChunkRadius;
        int radius = Math.Clamp(requestedRadius, 4, 22);
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
    }
}
