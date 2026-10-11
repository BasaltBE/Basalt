using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ClientCacheStatusHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, ClientCacheStatusPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.ClientCacheStatus");
        if (player is null)
            throw new InvalidDataException("Client cache status arrived before player login.");

        ResourcePack[] resourcePacks = network.ResourcePacks.Packs;
        PackInfoData[] packs = new PackInfoData[resourcePacks.Length];
        for (int i = 0; i < packs.Length; i++) {
            ResourcePack pack = resourcePacks[i];
            packs[i] = new PackInfoData {
                IdVersion = new PackIdVersion { PackId = pack.Id, Version = pack.Version },
                Size = (ulong)pack.ArchiveLength
            };
        }

        network.SendPacket(player, new ResourcePacksInfoPacket {
            ResourcePackRequired = network.ResourcePacks.Required,
            WorldTemplateIdVersion = new PackIdVersion { Version = "0.0.0" },
            ResourcePacks = packs
        });
        Logger.Scoped("Network").Debug(
            $"Sent resource pack info to {peer.ConnectionId}: {packs.Length} packs (client cache: {packet.SupportsCache})."
        );
    }
}
