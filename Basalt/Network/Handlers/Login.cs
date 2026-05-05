using Basalt.Core;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Login;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class Login
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        LoginPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (packet.Protocol != ProtocolInfo.ProtocolVersion)
        {
            DisconnectReason reason = packet.Protocol < ProtocolInfo.ProtocolVersion
                ? DisconnectReason.OutdatedClient
                : DisconnectReason.OutdatedServer;

            DisconnectPacket disconnect = new()
            {
                Reason = reason,
                HideDisconnectionScreen = true,
                Message = "",
                FilteredMessage = ""
            };

            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            Console.WriteLine($"Login rejected protocol={packet.Protocol} expected={ProtocolInfo.ProtocolVersion}");
            return;
        }


        var identity = LoginIdentityVerifier.Verify(packet.Identity);
        _ = LoginPayload.Parse(packet.Client);



        PlayStatusPacket status = new()
        {
            Status = PlayStatus.LoginSuccess,
        };

        ResourcePacksInfoPacket resources = new()
        {
            MustAccept = false,
            HasAddons = false,
            HasScripts = false,
            ForceDisableVibrantVisuals = false,
            WorldTemplateUuid = Guid.Empty,
            WorldTemplateVersion = "",
            Packs = []
        };

        server.Network.SendPackets(connection, [status, resources]);



        var player = new Player(identity.Username, identity.Xuid, identity.Uuid);
        server.Players[connection] = player;

        Console.WriteLine($"Player {identity.Username} has logged in!");
    }
}
