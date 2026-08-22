namespace Basalt.Core.Network.Handlers;

using System.Text;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.BedrockProtocol.Packets;

public static class ResourcePackChunkRequest {
    public static void Handle(Server server, NetworkConnection connection, ResourcePackChunkRequestPacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ResourcePackChunkRequest.Handle") : default;

        ResourcePack? pack = server.ResourcePacks.GetByUuid(packet.ResourceName);
        if (pack is null) {
            Logger.Warn($"Client requested unknown resource pack: {packet.ResourceName}");
            return;
        }

        uint chunkSize = server.ResourcePacks.ChunkSize;
        ulong dataOffset = (ulong)packet.Chunk * chunkSize;

        if (dataOffset >= pack.Size) {
            Logger.Warn($"Client requested out-of-range chunk {packet.Chunk} for pack {pack.Name}.");
            return;
        }

        ulong end = Math.Min(dataOffset + chunkSize, pack.Size);
        int length = (int)(end - dataOffset);

        ResourcePackChunkDataPacket response = new() {
            ResourceName = pack.Uuid.ToString(),
            ChunkId = (uint)packet.Chunk,
            ByteOffset = dataOffset,
            ChunkData = Encoding.Latin1.GetString(pack.Data.AsSpan((int)dataOffset, length)),
        };

        server.Network.QueuePacket(connection, response);
    }
}
