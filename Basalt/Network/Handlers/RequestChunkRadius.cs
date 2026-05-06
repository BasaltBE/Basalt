using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Protocol.Enums;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.Network.Handlers;

// TODO Turn this into a trait
public static class RequestChunkRadius
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestChunkRadiusPacket packet = new();
        packet.Deserialize(packetBuffer);

        int radius = Math.Clamp(packet.ChunkRadius, 1, 12);
        ChunkRadiusUpdatedPacket response = new()
        {
            ChunkRadius = radius
        };
        server.Network.SendPacket(connection, response);

        NetworkChunkPublisherUpdatePacket publisherUpdate = new()
        {
            CoordinateX = 0,
            CoordinateY = -59,
            CoordinateZ = 0,
            Radius = (uint)(radius << 4),
            SavedChunks = []
        };
        server.Network.SendPacket(connection, publisherUpdate);

        var dimension = server.World.GetDimension(DimensionType.Overworld);
        if (dimension is null)
        {
            return;
        }

        int centerChunkX = FloorDiv(publisherUpdate.CoordinateX, 16);
        int centerChunkZ = FloorDiv(publisherUpdate.CoordinateZ, 16);
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int chunkX = centerChunkX + dx;
                int chunkZ = centerChunkZ + dz;
                ChunkColumn chunk = dimension.GetOrCreateChunk(chunkX, chunkZ);

                LevelChunkPacket levelChunk = new()
                {
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    Dimension = 0,
                    SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                    CacheEnabled = false,
                    RawPayload = ChunkColumn.Serialize(chunk)
                };
                server.Network.SendPacket(connection, levelChunk);
            }
        }
    }

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
        {
            quotient--;
        }

        return quotient;
    }
}
