namespace Basalt.Core.Network.Handlers;

using Basalt.Binary;
using Basalt.Core;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Login;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Protocol.Types;
using Basalt.Protocol.Login.Data;
using Basalt.Protocol.Nbt;
using System.Security.Cryptography;
using System.Text;


public static class Login
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        using var __zone = Profiler.BeginZone("Login.Handle");
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

        KeyValuePair<NetworkConnection, Player.Player>? existingPlayerSession = null;
        foreach ((NetworkConnection existingConnection, Player.Player existingPlayer) in server.Players)
        {
            bool sameXuid = !string.IsNullOrWhiteSpace(identity.Xuid) &&
                string.Equals(existingPlayer.Xuid, identity.Xuid, StringComparison.Ordinal);
            bool sameUsername = string.Equals(existingPlayer.Username, identity.Username, StringComparison.OrdinalIgnoreCase);

            if (!sameXuid && !sameUsername)
            {
                continue;
            }

            existingPlayerSession = new KeyValuePair<NetworkConnection, Player.Player>(existingConnection, existingPlayer);
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

        Guid playerUuid = ResolvePlayerUuid(identity.Uuid, clientData.SelfSignedId, identity.Username, server.Properties.OnlineMode);
        string playerXuid = ResolvePlayerXuid(identity.Xuid, playerUuid, server.Properties.OnlineMode);
        var player = new Player.Player(identity.Username, playerXuid, playerUuid);
        var world = server.GetWorld();
        var savedData = LoadPlayerDataCompat(world, playerXuid, identity.Xuid, identity.Username, playerUuid);
        if (savedData is not null)
        {
            player.Read(savedData);
            if (!string.Equals(playerXuid, identity.Xuid, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(playerXuid))
            {
                world.Provider.SavePlayerData(playerXuid, savedData);
            }
        }

        bool isOperator = (savedData?.Get<ByteTag>("isOp")?.Value ?? 0) != 0;
        player.SetOperator(isOperator, syncClient: false);

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
            if (server.Properties.OnlineMode)
            {
                throw new InvalidOperationException("Offline authentication is disabled.");
            }

            return OfflineIdentity.VerifyOffline(envelope, packet.Client);
        }

        return LoginIdentity.Verify(packet.Identity);
    }

    private static Guid ResolvePlayerUuid(string identityUuid, string selfSignedId, string username, bool onlineMode)
    {
        if (Guid.TryParse(identityUuid, out Guid parsedIdentity))
        {
            return parsedIdentity;
        }

        if (Guid.TryParse(selfSignedId, out Guid parsedSelfSigned))
        {
            return parsedSelfSigned;
        }

        if (!onlineMode)
        {
            return CreateOfflineGuid(username);
        }

        return Guid.NewGuid();
    }

    private static string ResolvePlayerXuid(string identityXuid, Guid uuid, bool onlineMode)
    {
        if (onlineMode && !string.IsNullOrWhiteSpace(identityXuid))
        {
            return identityXuid;
        }

        return uuid.ToString("N");
    }

    private static Guid CreateOfflineGuid(string username)
    {
        string normalized = username.Trim().ToLowerInvariant();
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes("basalt:offline:" + normalized));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    private static CompoundTag? LoadPlayerDataCompat(
        Worlds.World world,
        string primaryXuid,
        string identityXuid,
        string username,
        Guid uuid)
    {
        var provider = world.Provider;

        // Try each candidate key with a raw byte lookup (no NBT deserialization).
        // Only deserialize the first key that actually has data.
        ReadOnlySpan<string> candidates =
        [
            primaryXuid,
            identityXuid,
            uuid.ToString("N"),
            uuid.ToString(),
            username
        ];

        string? previous1 = null;
        string? previous2 = null;

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            // Skip duplicates
            if (string.Equals(candidate, previous1, StringComparison.Ordinal) ||
                string.Equals(candidate, previous2, StringComparison.Ordinal))
            {
                continue;
            }

            byte[]? raw = provider.GetRawPlayerData(candidate);
            if (raw is not null)
            {
                return provider.LoadPlayerDataFromRaw(raw);
            }

            previous2 = previous1;
            previous1 = candidate;
        }

        return null;
    }
}









