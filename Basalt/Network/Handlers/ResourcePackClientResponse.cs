namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.RakNet;
using BedrockProtocol.Packets;
using BedrockProtocol.Enums;
using BedrockProtocol.Types;

public static class ResourcePackClientResponse {
    public static void Handle(Server server, NetworkConnection connection, ResourcePackClientResponsePacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ResourcePackResponse.Handle") : default;
        // Logger.Info($"Response: {packet.Response}");

        if (packet.Response is ResourcePackClientResponseDownloading initialDownloading
            && initialDownloading.DownloadingPacks.Count == 1
            && initialDownloading.DownloadingPacks[0] == "downloadingfinished") {
            packet.Response = new ResourcePackClientResponseDownloadingFinished();
        }

        switch (packet.Response) {
            case ResourcePackClientResponseCancel:
                if (server.Properties.ForceResourcePacks) {
                    DisconnectPacket disconnect = new() {
                        Reason = DisconnectFailReason.ResourcePackProblem,
                        Messages = new() {
                            Message = "Required resource packs were refused.",
                            FilteredMessage = "Required resource packs were refused."
                        }
                    };
                    Logger.Warn("Session failed due to Required resource packs were refused.");
                    server.Network.QueuePacket(connection, disconnect);
                }
                return;

            case ResourcePackClientResponseDownloading downloading:
                foreach (string packId in downloading.DownloadingPacks) {
                    ResourcePack? pack = server.ResourcePacks.GetByUuid(packId);
                    if (pack is null) {
                        Logger.Warn($"Client requested unknown pack: {packId}");
                        continue;
                    }

                    uint chunkSize = server.ResourcePacks.ChunkSize;
                    ResourcePackDataInfoPacket dataInfo = new() {
                        FileHash = pack.Hash.ToString() ?? "",
                        FileSize = pack.Size,
                        IsPremiumPack = false,
                        NumberOfChunks = pack.ChunkCount(chunkSize),
                        ChunkSize = chunkSize,
                        PackType = 6,
                        ResourceName = pack.Uuid.ToString() ?? ""
                    };
                    server.Network.QueuePacket(connection, dataInfo);
                }
                return;

            case ResourcePackClientResponseDownloadingFinished finished:
                List<PackInstanceId> stackPacks =
                [
                    new PackInstanceId {
                        Version = "1.0.0",
                        SubPackName = "",
                        PackID = "0fba4063-dba1-4281-9b89-ff9390653530",
                    }
                ];

                foreach (ResourcePack loadedPack in server.ResourcePacks.Packs) {
                    stackPacks.Add(new PackInstanceId {
                        PackID = loadedPack.Uuid.ToString(),
                        Version = loadedPack.VersionString,
                        SubPackName = "Education Edition Resource Pack"
                    });
                }

                ResourcePackStackPacket stack = new() {
                    TexturePackRequired = server.Properties.ForceResourcePacks,
                    BaseGameVersion = Constants.MinecraftVersion,
                    IncludeEditorPacks = true,
                    TexturePackList = stackPacks,
                    Experiments = new() {
                        ExperimentsEverToggled = false,
                        Toggles = [],
                    },
                };
                server.Network.QueuePacket(connection, stack);
                return;

            case ResourcePackClientResponseResourcePackStackFinished:
                if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
                    Console.WriteLine("Resource pack flow completed, but no player session was found.");
                    DisconnectPacket missingSessionDisconnect = new() {
                        Reason = DisconnectFailReason.Disconnected,
                        Messages = new() {
                            Message = "Server force closed the connection.",
                            FilteredMessage = "Server force closed the connection."
                        }
                    };
                    server.Network.QueuePacket(connection, missingSessionDisconnect);
                    connection.Disconnect();
                    return;
                }

                server.Scheduler.Schedule(new ResourcePackCompletedTask(server, connection, player));
                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {packet.Response}");
                return;
        }
    }
}










