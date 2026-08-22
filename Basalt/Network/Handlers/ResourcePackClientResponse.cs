namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public static class ResourcePackClientResponse {
    public static void Handle(Server server, NetworkConnection connection, ResourcePackClientResponsePacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ResourcePackResponse.Handle") : default;

        switch (packet.Response) {
            case ResourcePackResponse.Cancel:
                if (!server.Properties.ForceResourcePacks) {
                    return;
                }

                server.Network.QueuePacket(connection, new DisconnectPacket {
                    Reason = DisconnectFailReason.ResourcePackProblem,
                    Messages = new() {
                        Message = "Required resource packs were refused.",
                        FilteredMessage = "Required resource packs were refused."
                    }
                });
                return;

            case ResourcePackResponse.Downloading:
                SendPackData(server, connection, packet.DownloadingPacks);
                return;

            case ResourcePackResponse.DownloadingFinished:
                SendPackStack(server, connection);
                return;

            case ResourcePackResponse.ResourcePackStackFinished:
                if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
                    connection.Disconnect();
                    return;
                }

                server.Scheduler.Schedule(new ResourcePackCompletedTask(server, connection, player));
                return;
        }
    }

    private static void SendPackData(Server server, NetworkConnection connection, string[] packIds) {
        foreach (string packId in packIds) {
            ResourcePack? pack = server.ResourcePacks.GetByUuid(packId);
            if (pack is null) {
                Logger.Warn($"Client requested unknown pack: {packId}");
                continue;
            }

            uint chunkSize = server.ResourcePacks.ChunkSize;
            server.Network.QueuePacket(connection, new ResourcePackDataInfoPacket {
                FileHash = Convert.ToHexString(pack.Hash),
                FileSize = pack.Size,
                IsPremiumPack = false,
                NumberOfChunks = pack.ChunkCount(chunkSize),
                ChunkSize = chunkSize,
                PackType = 6,
                ResourceName = pack.Uuid.ToString()
            });
        }
    }

    private static void SendPackStack(Server server, NetworkConnection connection) {
        List<PackInstanceId> packs = [new() {
            Version = "1.0.0",
            SubPackName = string.Empty,
            PackId = "0fba4063-dba1-4281-9b89-ff9390653530"
        }];

        packs.AddRange(server.ResourcePacks.Packs.Select(static pack => new PackInstanceId {
            PackId = pack.Uuid.ToString(),
            Version = pack.VersionString,
            SubPackName = "Education Edition Resource Pack"
        }));

        server.Network.QueuePacket(connection, new ResourcePackStackPacket {
            TexturePackRequired = server.Properties.ForceResourcePacks,
            BaseGameVersion = Constants.MinecraftVersion,
            IncludeEditorPacks = true,
            TexturePackList = [.. packs],
            Experiments = new() {
                ExperimentsEverToggled = false,
                Toggles = []
            }
        });
    }
}
