using Basalt.Core;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ResourcePackClientResponse
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ResourcePackClientResponsePacket packet = new();
        packet.Deserialize(packetBuffer);

        switch (packet.Response)
        {
            case ResourcePackResponse.Refused:
                DisconnectPacket disconnect = new()
                {
                    Reason = DisconnectReason.ResourcePackProblem,
                    HideDisconnectionScreen = false,
                    Message = "Required resource packs were refused.",
                    FilteredMessage = "Required resource packs were refused."
                };
                server.Network.SendPacket(connection, disconnect);
                return;

            case ResourcePackResponse.SendPacks:
                Console.WriteLine($"Client requested packs ({packet.PacksToDownload.Count}). Pack transfer is not implemented yet.");
                return;

            case ResourcePackResponse.AllPacksDownloaded:
                ResourcePackStackPacket stack = new()
                {
                    MustAccept = false,
                    Packs =
                    [
                        new ResourcePackStackEntry
                        {
                            Uuid = Guid.Parse("0fba4063-dba1-4281-9b89-ff9390653530"),
                            Version = "1.0.0",
                            SubPackName = string.Empty
                        }
                    ],
                    BaseGameVersion = ProtocolInfo.MinecraftVersion,
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    IncludeEditorPacks = true
                };
                server.Network.SendPacket(connection, stack);
                return;

            case ResourcePackResponse.Completed:
                Console.WriteLine("Resource pack flow completed.");
                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {(byte)packet.Response}");
                return;
        }
    }
}
