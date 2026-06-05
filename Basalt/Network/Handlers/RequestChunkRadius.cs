namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Player.Traits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class RequestChunkRadius
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestChunkRadiusPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (RequestChunkRadiusPacket)Protocol.Io.Packet.Deserialize(reader);

        int requestedRadius = packet.ChunkRadius;
        int maxViewDistance = Math.Clamp(server.Properties.MaxViewDistance, 4, 120);
        int radius = Math.Clamp(requestedRadius, 4, maxViewDistance);
        // UpdateChunkRadiusPacket response = new()
        // {
        //     ChunkRadius = radius
        // };  

        // THIS STUPID PACKET CRASHES MOBILE DEVICES!!!
        /// PLEASE KEEP IT COMMENTED OUT!
        // server.Network.SendPacket(connection, response);

        if (!server.Players.TryGetValue(connection, out Player.Player? player))
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










