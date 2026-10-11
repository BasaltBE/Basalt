using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ResourcePackChunkRequestHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, ResourcePackChunkRequestPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.ResourcePackChunkRequest");
        if (player is null || packet.Chunk < 0 ||
            !network.ResourcePacks.TryGet(packet.ResourceName, out ResourcePack? pack) || pack is null ||
            !player.RequestedResourcePacks.Contains(pack.Id))
            throw new InvalidDataException("Client requested a resource pack chunk that was not offered.");

        uint chunk = (uint)packet.Chunk;
        ReadOnlyMemory<byte> chunkData = pack.GetChunk(chunk, ResourcePackManager.ChunkSize);
        network.SendPacket(player, new ResourcePackChunkDataPacket {
            ResourceName = pack.ResourceName,
            ChunkId = chunk,
            ByteOffset = (ulong)chunk * ResourcePackManager.ChunkSize,
            ChunkData = chunkData
        }, method: PacketCompressionAlgorithm.None);
    }
}
