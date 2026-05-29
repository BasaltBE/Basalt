using Basalt.Binary;
using Basalt.Core;
using Basalt.Events;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Login;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Protocol.Types;
using Basalt.Protocol.Login.Data;
using Basalt.Protocol.Nbt;

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
            return;
        }

        VerifiedIdentity identity;
        try
        {
            identity = VerifyIdentity(server, packet);
        }
        catch (Exception exception)
        {
            Logger.Info($"Login rejected: {exception.Message}");
            string message = exception.Message switch
            {
                "Offline authentication is disabled." =>
                    "Offline mode is not supported. Please connect to Xbox services.",
                _ => "Authentication failed."
            };

            DisconnectPacket disconnect = new()
            {
                Reason = DisconnectReason.Disconnected,
                HideDisconnectionScreen = false,
                Message = message,
                FilteredMessage = message
            };

            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        ClientData clientData = LoginPayload.Parse(packet.Client);

        KeyValuePair<NetworkConnection, Player>? existingPlayerSession = null;
        foreach ((NetworkConnection existingConnection, Player existingPlayer) in server.Players)
        {
            bool sameXuid = !string.IsNullOrWhiteSpace(identity.Xuid) &&
                string.Equals(existingPlayer.Xuid, identity.Xuid, StringComparison.Ordinal);
            bool sameUsername = string.Equals(existingPlayer.Username, identity.Username, StringComparison.OrdinalIgnoreCase);

            if (!sameXuid && !sameUsername)
            {
                continue;
            }

            existingPlayerSession = new KeyValuePair<NetworkConnection, Player>(existingConnection, existingPlayer);
            break;
        }

        if (existingPlayerSession.HasValue)
        {
            DisconnectPacket duplicateDisconnect = new()
            {
                Reason = DisconnectReason.Disconnected,
                HideDisconnectionScreen = false,
                Message = "Logged in from another location.",
                FilteredMessage = "Logged in from another location."
            };

            server.Network.SendPacket(existingPlayerSession.Value.Key, duplicateDisconnect, CompressionMethod.NotPresent);
            existingPlayerSession.Value.Key.Disconnect();
        }

        Guid playerUuid = ResolvePlayerUuid(identity.Uuid, clientData.SelfSignedId);
        var player = new Player(identity.Username, identity.Xuid, playerUuid);
        var world = server.GetWorld();
        var savedData = world.Provider.LoadPlayerData(identity.Xuid);
        if (savedData is not null)
        {
            player.FromNBT(savedData);
        }

        bool isOperator = world.Operators.IsOperator(identity.Xuid)
            || (savedData?.Get<ByteTag>("isOp")?.Value ?? 0) != 0;
        if (isOperator && !world.Operators.IsOperator(identity.Xuid))
        {
            world.Operators.AddOperator(identity.Xuid);
        }
        player.SetOperator(isOperator, syncClient: false);

        world.PlayerProfiles.UpdateIndex(identity.Username, identity.Xuid);

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
        player.DeviceOS = clientData.DeviceOs;
        player.SetSkin(Skin.FromClientData(clientData));
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

    private static VerifiedIdentity VerifyIdentity(Server server, LoginPacket packet)
    {
        LoginEnvelope envelope = LoginEnvelope.Parse(packet.Identity);

        if (OfflineIdentity.IsOfflineLogin(envelope))
        {
            if (!server.Options.OfflineMode)
            {
                throw new InvalidOperationException("Offline authentication is disabled.");
            }

            return OfflineIdentity.VerifyOffline(envelope, packet.Client);
        }

        return LoginIdentity.Verify(packet.Identity);
    }

    private static Guid ResolvePlayerUuid(string identityUuid, string selfSignedId)
    {
        if (Guid.TryParse(identityUuid, out Guid parsedIdentity))
        {
            return parsedIdentity;
        }

        if (Guid.TryParse(selfSignedId, out Guid parsedSelfSigned))
        {
            return parsedSelfSigned;
        }

        return Guid.NewGuid();
    }
}
