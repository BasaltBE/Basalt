namespace Basalt.Core.Network.Handlers;

using System.Security.Cryptography;
using System.Text;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Login;
using Basalt.Protocol.Login.Data;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;
using PermissionEntry = Basalt.Core.Player.PermissionEntry;

internal sealed class LoginTask : ServerTask {
    private readonly Server _server;
    private readonly NetworkConnection _connection;
    private readonly LoginPacket _packet;

    private VerifiedIdentity _identity;
    private ClientData _clientData;
    private Player.Player? _player;
    private string? _rejectMessage;

    public LoginTask(Server server, NetworkConnection connection, LoginPacket packet) {
        _server = server;
        _connection = connection;
        _packet = packet;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("LoginTask.Execute") : default;

        try {
            _identity = VerifyIdentity(_server, _packet);
        }
        catch (Exception exception) {
            Logger.Info($"Login rejected: {exception.Message}");
            _rejectMessage = exception.Message switch {
                "Offline authentication is disabled." =>
                    "Offline mode is not supported. Please connect to Xbox services.",
                _ => "Authentication failed."
            };
            return;
        }

        ClientData clientData = LoginPayload.Parse(_packet.Client);
        _clientData = clientData;

        Guid playerUuid = ResolvePlayerUuid(
            _identity.Uuid, clientData.SelfSignedId, _identity.Username, _server.Properties.OnlineMode);
        string playerXuid = ResolvePlayerXuid(_identity.Xuid, playerUuid, _server.Properties.OnlineMode);

        _player = new Player.Player(_identity.Username, playerXuid, playerUuid);

        Worlds.World world = _server.GetWorld();
        CompoundTag? savedData = LoadPlayerDataCompat(
            world, playerXuid, _identity.Xuid, _identity.Username, playerUuid);

        if (savedData is not null) {
            _player.Read(savedData);

            bool shouldMigrateXuid = !string.Equals(playerXuid, _identity.Xuid, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(playerXuid);

            if (shouldMigrateXuid) {
                world.Persistence.SavePlayerData(playerXuid, savedData);
            }
        }

        PermissionEntry? permEntry = _server.PermissionStore.Get(playerXuid);
        if (permEntry is not null) {
            _player.Permissions.Restore(permEntry.IsOperator, permEntry.Permissions);
        }
        else {
            _player.SetOperator(false, syncClient: false);
        }

        _player.SetSkin(Skin.FromClientData(clientData));
    }

    public override void Complete() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("LoginTask.Complete") : default;

        if (_rejectMessage is not null) {
            DisconnectPacket disconnect = new() {
                Reason = DisconnectReason.Disconnected,
                HideDisconnectionScreen = false,
                Message = _rejectMessage,
                FilteredMessage = _rejectMessage
            };
            _server.Network.QueuePacket(_connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        Player.Player player = _player!;

        using (Profiler.Enabled ? Profiler.BeginZone("Login.KickDuplicate") : default) {
            KeyValuePair<NetworkConnection, Player.Player>? existingSession = null;
            foreach ((NetworkConnection existingConnection, Player.Player existingPlayer) in _server.Players) {
                bool sameXuid = !string.IsNullOrWhiteSpace(_identity.Xuid) &&
                    string.Equals(existingPlayer.Xuid, _identity.Xuid, StringComparison.Ordinal);
                bool sameUsername = string.Equals(
                    existingPlayer.Username, _identity.Username, StringComparison.OrdinalIgnoreCase);

                if (!sameXuid && !sameUsername) {
                    continue;
                }

                existingSession = new KeyValuePair<NetworkConnection, Player.Player>(existingConnection, existingPlayer);
                break;
            }

            if (existingSession.HasValue) {
                DisconnectPacket duplicateDisconnect = new() {
                    Reason = DisconnectReason.Disconnected,
                    HideDisconnectionScreen = false,
                    Message = "Logged in from another location.",
                    FilteredMessage = "Logged in from another location."
                };
                _server.Network.QueuePacket(existingSession.Value.Key, duplicateDisconnect, CompressionMethod.NotPresent);
                existingSession.Value.Key.Disconnect();
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Login.EmitJoin") : default) {
            PlayerJoinSignal joinSignal = new(player);
            _server.Emit(joinSignal);
            if (!joinSignal.Emit()) {
                DisconnectPacket disconnect = new() {
                    Reason = DisconnectReason.Disconnected,
                    HideDisconnectionScreen = false,
                    Message = "Server force closed the connection.",
                    FilteredMessage = "Server force closed the connection."
                };
                _server.Network.QueuePacket(_connection, disconnect, CompressionMethod.NotPresent);
                _connection.Disconnect();
                return;
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Login.RegisterPlayer") : default) {
            player.Connection = _connection;
            player.Network = _server.Network;
            player.DeviceOS = _clientData.DeviceOs;
            _server.Players[_connection] = player;
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Login.SendResponse") : default) {
            PlayStatusPacket status = new(PlayStatus.LoginSuccess);

            ResourcePacksInfoPacket resources = new() {
                MustAccept = _server.Properties.ForceResourcePacks,
                HasAddons = false,
                HasScripts = false,
                ForceDisableVibrantVisuals = false,
                WorldTemplateUuid = Guid.Empty,
                WorldTemplateVersion = "",
                Packs = _server.ResourcePacks.Packs.Select(static pack => new ResourcePackInfo {
                    Uuid = pack.Uuid,
                    Version = pack.VersionString,
                    Size = pack.Size,
                    ContentKey = "",
                    SubPackName = "",
                    ContentIdentity = "",
                    HasScripts = false,
                    HasAddons = false,
                    RtxEnabled = false,
                    DownloadUrl = ""
                }).ToList()
            };

            _server.Network.QueuePackets(_connection, [status, resources]);
        }

        Logger.Info($"Player {_identity.Username} has logged in!");
    }

    private static VerifiedIdentity VerifyIdentity(Server server, LoginPacket packet) {
        LoginEnvelope envelope = LoginEnvelope.Parse(packet.Identity);
        bool offlineLogin = OfflineIdentity.IsOfflineLogin(envelope)
            || envelope.AuthenticationType == 2;

        if (offlineLogin) {
            if (server.Properties.OnlineMode) {
                throw new InvalidOperationException("Offline authentication is disabled.");
            }

            return OfflineIdentity.VerifyOffline(envelope, packet.Client);
        }

        return LoginIdentity.Verify(packet.Identity);
    }

    private static Guid ResolvePlayerUuid(string identityUuid, string selfSignedId, string username, bool onlineMode) {
        if (Guid.TryParse(identityUuid, out Guid parsedIdentity)) {
            return parsedIdentity;
        }

        if (Guid.TryParse(selfSignedId, out Guid parsedSelfSigned)) {
            return parsedSelfSigned;
        }

        if (!onlineMode) {
            return CreateOfflineGuid(username);
        }

        return Guid.NewGuid();
    }

    private static string ResolvePlayerXuid(string identityXuid, Guid uuid, bool onlineMode) {
        if (onlineMode && !string.IsNullOrWhiteSpace(identityXuid)) {
            return identityXuid;
        }

        return uuid.ToString("N");
    }

    private static Guid CreateOfflineGuid(string username) {
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
        Guid uuid) {
        Worlds.Dimensions.Provider.WorldProvider provider = world.Provider;

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

        foreach (string candidate in candidates) {
            if (string.IsNullOrWhiteSpace(candidate)) {
                continue;
            }

            if (string.Equals(candidate, previous1, StringComparison.Ordinal) ||
                string.Equals(candidate, previous2, StringComparison.Ordinal)) {
                continue;
            }

            CompoundTag? pending = world.Persistence.GetPendingPlayerData(candidate);
            if (pending is not null) {
                return pending;
            }

            byte[]? raw = provider.GetRawPlayerData(candidate);
            if (raw is not null) {
                return provider.LoadPlayerDataFromRaw(raw);
            }

            previous2 = previous1;
            previous1 = candidate;
        }

        return null;
    }
}
