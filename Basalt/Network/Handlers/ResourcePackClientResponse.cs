namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Protocol.Io;
using Basalt.Core.Blocks;


public static class ResourcePackClientResponse {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        using var __zone = Profiler.BeginZone("ResourcePackResponse.Handle");
        ResourcePackClientResponsePacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ResourcePackClientResponsePacket)Protocol.Io.Packet.Deserialize(reader);

        switch (packet.Response) {
            case ResourcePackResponse.Refused:
                if (server.Properties.ForceResourcePacks) {
                    DisconnectPacket disconnect = new() {
                        Reason = DisconnectReason.ResourcePackProblem,
                        HideDisconnectionScreen = false,
                        Message = "Required resource packs were refused.",
                        FilteredMessage = "Required resource packs were refused."
                    };
                    server.Network.SendPacket(connection, disconnect);
                }
                return;

            case ResourcePackResponse.SendPacks:
                foreach (string packId in packet.PacksToDownload) {
                    ResourcePack? pack = server.ResourcePacks.GetByUuid(packId);
                    if (pack is null) {
                        Logger.Warn($"Client requested unknown pack: {packId}");
                        continue;
                    }

                    uint chunkSize = server.ResourcePacks.ChunkSize;
                    ResourcePackDataInfoPacket dataInfo = new() {
                        Uuid = pack.Uuid.ToString(),
                        ChunkSize = chunkSize,
                        ChunkCount = pack.ChunkCount(chunkSize),
                        Size = pack.Size,
                        Hash = pack.Hash,
                        Premium = false,
                        PackType = 6
                    };
                    server.Network.SendPacket(connection, dataInfo);
                }
                return;

            case ResourcePackResponse.AllPacksDownloaded:
                List<ResourcePackStackEntry> stackPacks =
                [
                    new ResourcePackStackEntry
                    {
                        Uuid = Guid.Parse("0fba4063-dba1-4281-9b89-ff9390653530"),
                        Version = "1.0.0",
                        SubPackName = ""
                    }
                ];

                foreach (ResourcePack loadedPack in server.ResourcePacks.Packs) {
                    stackPacks.Add(new ResourcePackStackEntry {
                        Uuid = loadedPack.Uuid,
                        Version = loadedPack.VersionString,
                        SubPackName = "Education Edition Resource Pack"
                    });
                }

                ResourcePackStackPacket stack = new() {
                    MustAccept = server.Properties.ForceResourcePacks,
                    Packs = stackPacks,
                    BaseGameVersion = Constants.MinecraftVersion,
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    IncludeEditorPacks = true
                };
                server.Network.SendPacket(connection, stack);
                return;

            case ResourcePackResponse.Completed:
                if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
                    Console.WriteLine("Resource pack flow completed, but no player session was found.");
                    DisconnectPacket missingSessionDisconnect = new() {
                        Reason = DisconnectReason.Disconnected,
                        HideDisconnectionScreen = false,
                        Message = "Server force closed the connection.",
                        FilteredMessage = "Server force closed the connection."
                    };
                    server.Network.SendPacket(connection, missingSessionDisconnect);
                    connection.Disconnect();
                    return;
                }

                server.Scheduler.Schedule(new ResourcePackCompletedTask(server, connection, player));
                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {(byte)packet.Response}");
                return;
        }
    }
}










