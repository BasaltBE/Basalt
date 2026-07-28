namespace Basalt.Core.Network.Handlers;

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
            Console.WriteLine($"Login: {_identity.Username} ({_identity.Xuid} | {_identity.Uuid})");
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
            _identity.Uuid,
            clientData.SelfSignedId,
            _identity.Username,
            !_server.Properties.OnlineMode);
        string playerXuid = ResolvePlayerXuid(_identity.Xuid, _identity.Username);

        _player = new Player.Player(_identity.Username, playerXuid, playerUuid);

        (Worlds.World World, CompoundTag Data)? savedPlayer = LoadPlayerDataCompat(
            _server, playerXuid, _identity.Xuid, _identity.Username, playerUuid);

        if (savedPlayer is not null) {
            _player.Read(savedPlayer.Value.Data);

            bool shouldMigrateXuid = !string.Equals(playerXuid, _identity.Xuid, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(playerXuid);

            if (shouldMigrateXuid) {
                savedPlayer.Value.World.Persistence.SavePlayerData(playerXuid, savedPlayer.Value.Data);
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

        if (_player is null && _rejectMessage is null) {
            _rejectMessage = "Login processing failed.";
        }

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

    private static Guid ResolvePlayerUuid(
        string identityUuid,
        string selfSignedId,
        string username,
        bool offline) {
        if (Guid.TryParse(identityUuid, out Guid playerUuid)) {
            return playerUuid;
        }

        if (Guid.TryParse(selfSignedId, out playerUuid)) {
            return playerUuid;
        }

        if (offline) {
            return OfflineIdentity.GetUuidFromUsername(username);
        }

        throw new InvalidOperationException("The login identity does not contain a valid UUID.");
    }

    private static string ResolvePlayerXuid(string identityXuid, string username) {
        if (!string.IsNullOrWhiteSpace(identityXuid)) {
            return identityXuid;
        }

        return OfflineIdentity.GetOfflineXuid(username);
    }

    private static (Worlds.World World, CompoundTag Data)? LoadPlayerDataCompat(
        Server server,
        string primaryXuid,
        string identityXuid,
        string username,
        Guid uuid) {
        ReadOnlySpan<string> candidates =
        [
            primaryXuid,
            identityXuid,
            uuid.ToString("N"),
            uuid.ToString(),
            username
        ];

        foreach (Worlds.World world in server.Worlds) {
            Worlds.Dimensions.Provider.WorldProvider provider = world.Provider;

            foreach (string candidate in candidates) {
                if (string.IsNullOrWhiteSpace(candidate)) {
                    continue;
                }

                CompoundTag? pending = world.Persistence.GetPendingPlayerData(candidate);
                if (pending is not null) {
                    return (world, pending);
                }

                byte[]? raw = provider.GetRawPlayerData(candidate);
                if (raw is not null && provider.LoadPlayerDataFromRaw(raw) is { } data) {
                    return (world, data);
                }

            }
        }

        return null;
    }
}
