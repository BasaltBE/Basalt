using Basalt.Binary;
using Basalt.Core;
using Basalt.Events;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Login;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class Login
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        LoginPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (LoginPacket)Protocol.Io.Packet.Deserialize(reader);

        if (packet.Protocol != Constants.ProtocolVersion)
        {
            DisconnectReason reason = packet.Protocol < Constants.ProtocolVersion
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
            Console.WriteLine($"Login rejected protocol={packet.Protocol} expected={Constants.ProtocolVersion}");
            return;
        }


        var identity = LoginIdentity.Verify(packet.Identity);
        _ = LoginPayload.Parse(packet.Client);



        var player = new Player(identity.Username, identity.Xuid, identity.Uuid);
        var savedData = server.GetWorld().Provider.LoadPlayerData(identity.Xuid);
        if (savedData is not null)
        {
            player.FromNBT(savedData);
        }

        PlayerJoinSignal joinSignal = new(player);
        server.Emit(joinSignal);
        if (!joinSignal.Emit())
        {
            DisconnectPacket disconnect = new()
            {
                Reason = DisconnectReason.Disconnected,
                HideDisconnectionScreen = false,
                Message = "Server force closed the connection.",
                FilteredMessage = "Server force closed the connection."
            };
            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            connection.Disconnect();
            return;
        }

        player.Connection = connection;
        player.Network = server.Network;
        server.Players[connection] = player;

        PlayStatusPacket status = new(PlayStatus.LoginSuccess);

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

        Logger.Info($"Player {identity.Username} has logged in!");
    }
}

