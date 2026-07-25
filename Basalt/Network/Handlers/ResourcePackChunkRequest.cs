namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class ResourcePackChunkRequest {
    public static void Handle(Server server, NetworkConnection connection, ResourcePackChunkRequestPacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ResourcePackChunkRequest.Handle") : default;

        ResourcePack? pack = server.ResourcePacks.GetByUuid(packet.Uuid);
        if (pack is null) {
            Logger.Warn($"Client requested unknown resource pack: {packet.Uuid}");
            return;
        }

        uint chunkSize = server.ResourcePacks.ChunkSize;
        ulong dataOffset = (ulong)packet.ChunkIndex * chunkSize;

        if (dataOffset >= pack.Size) {
            Logger.Warn($"Client requested out-of-range chunk {packet.ChunkIndex} for pack {pack.Name}.");
            return;
        }

        ulong end = Math.Min(dataOffset + chunkSize, pack.Size);
        int length = (int)(end - dataOffset);

        ResourcePackChunkDataPacket response = new() {
            Uuid = pack.Uuid.ToString(),
            ChunkIndex = packet.ChunkIndex,
            DataOffset = dataOffset,
            Data = pack.Data.AsSpan((int)dataOffset, length).ToArray()
        };

        server.Network.QueuePacket(connection, response);
    }
}
