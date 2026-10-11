using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ResourcePackClientResponseHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, ResourcePackClientResponsePacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.ResourcePackResponse");
        if (player is null)
            throw new InvalidDataException("Resource pack response arrived before player login.");

        switch (packet.Response) {
            case ResourcePackClientResponseDownloading downloading:
                SendPackData(player, downloading);
                break;
            case ResourcePackClientResponseDownloadingFinished:
                SendStack(player, includePacks: true);
                break;
            case ResourcePackClientResponseStackFinished:
                Logger.Scoped("Network").Debug($"Resource packs applied by {peer.ConnectionId}.");
                break;
            case ResourcePackClientResponseCancel:
                if (network.ResourcePacks.Required) {
                    const string message = "This server requires its resource packs.";
                    network.SendPacket(player, new DisconnectPacket {
                        Reason = DisconnectFailReason.Unknown,
                        Message = message,
                        FilteredMessage = message
                    });
                    break;
                }

                player.RequestedResourcePacks.Clear();
                SendStack(player, includePacks: false);
                break;
            default:
                throw new InvalidDataException("Unknown resource pack response.");
        }
    }

    private void SendPackData(Player player, ResourcePackClientResponseDownloading downloading) {
        foreach (string resourceName in downloading.PackIds) {
            if (!network.ResourcePacks.TryGet(resourceName, out ResourcePack? pack) || pack is null)
                throw new InvalidDataException($"Client requested unknown resource pack '{resourceName}'.");

            player.RequestedResourcePacks.Add(pack.Id);
            network.SendPacket(player, new ResourcePackDataInfoPacket {
                ResourceName = pack.ResourceName,
                ChunkSize = ResourcePackManager.ChunkSize,
                NumberOfChunks = pack.GetChunkCount(ResourcePackManager.ChunkSize),
                FileSize = (ulong)pack.ArchiveLength,
                FileHash = pack.Hash,
                PackType = 6
            });
        }
    }

    private void SendStack(Player player, bool includePacks) {
        ResourcePack[] packs = includePacks ? network.ResourcePacks.Packs : [];
        PackInstanceId[] texturePacks = new PackInstanceId[packs.Length];
        for (int i = 0; i < packs.Length; i++) {
            ResourcePack pack = packs[i];
            texturePacks[i] = new PackInstanceId {
                PackId = pack.Id.ToString(),
                Version = pack.Version
            };
        }

        network.SendPacket(player, new ResourcePackStackPacket {
            TexturePackRequired = network.ResourcePacks.Required,
            TexturePacks = texturePacks,
            BaseGameVersion = BedrockProtocol.MinecraftVersion
        });
    }
}
