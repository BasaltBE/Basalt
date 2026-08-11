namespace Basalt.Core.Network.Handlers;

using Basalt.Binary;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
// using Basalt.Protocol.Enums;s
using BedrockProtocol.Nbt;
// using Basalt.Protocol.Packets;
// using Basalt.Protocol.Types;
using Basalt.RakNet;
using PermissionEntry = Basalt.Core.Player.PermissionEntry;

using BedrockProtocol.Packets;
using System.Text;
using BedrockProtocol.Enums;
using BedrockProtocol.Types;
using Basalt.Core.Network.Login.Data;
using Basalt.Core.Network.Login;

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

        byte[] bytes = Encoding.Latin1.GetBytes(_packet.ConnectionRequest);
        ReadOnlySpan<byte> data = bytes;

        int offset = 0;
        BinaryReader reader = new(data, ref offset);

        string identity = reader.ReadString32(true);
        int declaredClientLength = reader.ReadInt32(true);

        ReadOnlySpan<byte> clientBytes =
            reader.ReadBytes(reader.Remaining);

        string client = Encoding.UTF8.GetString(clientBytes);

        try {
            _identity = VerifyIdentity(_server, client, identity);
            // Console.WriteLine($"Login: {_identity.Username} ({_identity.Xuid} | {_identity.Uuid})");
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



        ClientData clientData = LoginPayload.Parse(client);
        _clientData = clientData;

        Guid playerUuid = ResolvePlayerUuid(
            _identity.Uuid,
            clientData.SelfSignedId,
            _identity.Username,
            !_server.Properties.OnlineMode);
        string playerXuid = ResolvePlayerXuid(_identity.Xuid, _identity.Username);

        _player = new Player.Player(_identity.Username, playerXuid, playerUuid);

        CompoundTag? savedPlayer = _server.PlayerData.Load(playerXuid) ?? LoadPlayerDataCompat(
            _server, playerXuid, _identity.Xuid, _identity.Username, playerUuid);

        if (savedPlayer is not null) {
            _player.Read(savedPlayer);

            bool shouldMigrateXuid = !string.Equals(playerXuid, _identity.Xuid, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(playerXuid);

            if (shouldMigrateXuid) {
                _server.PlayerData.Save(playerXuid, savedPlayer);
            }
        }

        PermissionEntry? permEntry = _server.PermissionStore.Get(playerXuid);
        if (permEntry is not null) {
            _player.Permissions.Restore(permEntry.IsOperator, permEntry.Permissions);
        }
        else {
            _player.SetOperator(false, syncClient: false);
        }

        _player.Skin = FromClientData(clientData); //(Protocol.Types.Skin.FromClientData(clientData));
    }

    public override void Complete() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("LoginTask.Complete") : default;

        if (_player is null && _rejectMessage is null) {
            _rejectMessage = "Login processing failed.";
        }

        if (_rejectMessage is not null) {
            DisconnectPacket disconnect = new() {
                Reason = DisconnectFailReason.Disconnected,
                Messages = new() {
                    FilteredMessage = _rejectMessage,
                    Message = _rejectMessage,
                }
                // Message = _rejectMessage,
                // FilteredMessage = _rejectMessage
            };
            _server.Network.QueuePacket(_connection, disconnect, Protocol.Enums.CompressionMethod.NotPresent);
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
                    Reason = DisconnectFailReason.Disconnected,
                    Messages = new() {
                        Message = "Logged in from another location.",
                        FilteredMessage = "Logged in from another location."
                    }
                };
                _server.Network.QueuePacket(existingSession.Value.Key, duplicateDisconnect, Protocol.Enums.CompressionMethod.NotPresent);
                existingSession.Value.Key.Disconnect();
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Login.EmitJoin") : default) {
            PlayerJoinSignal joinSignal = new(player);
            _server.Emit(joinSignal);
            if (!joinSignal.Emit()) {
                DisconnectPacket disconnect = new() {
                    Reason = DisconnectFailReason.Disconnected,
                    Messages = new() {
                        Message = "Server force closed the connection.",
                        FilteredMessage = "Server force closed the connection."
                    }
                };
                _server.Network.QueuePacket(_connection, disconnect, Protocol.Enums.CompressionMethod.NotPresent);
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
            PlayStatusPacket status = new() {
                Status = PlayStatus.LoginSuccess,
            };

            ResourcePacksInfoPacket resources = new() {
                ResourcePackRequired = _server.Properties.ForceResourcePacks,
                HasAddonPacks = false,
                HasScripts = false,
                ForceDisableVibrantVisuals = false,
                WorldTemplateIdAndVersion = new() {
                    PackUUID = new() { },
                    PackVersion = new() { Version = "" },
                },
                ResourcePacks = _server.ResourcePacks.Packs.Select(static pack => new PackInfoData {
                    CDNURL = "",
                    IsAddonPack = false,
                    IsRayTracingCapable = true,
                    PackIdVersion = new() {
                        PackUUID = FromGuid(pack.Uuid),
                        PackVersion = new() {
                            Version = pack.VersionString,
                        },
                    },
                    PackSize = pack.Size,
                    SubpackName = "",
                    ContentKey = "",
                    ContentIdentity = new() {
                        Identity = "",
                    },
                    HasScripts = false,
                }).ToList()
            };

            _server.Network.QueuePackets(_connection, [status, resources]);
        }

        Logger.Info($"Player {_identity.Username} has logged in!");
    }

    private static VerifiedIdentity VerifyIdentity(
        Server server,
        string clientBytes,
        string identityBytes
    ) {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityBytes);
        bool offlineLogin = OfflineIdentity.IsOfflineLogin(envelope)
            || envelope.AuthenticationType == 2;

        if (offlineLogin) {
            if (server.Properties.OnlineMode) {
                throw new InvalidOperationException("Offline authentication is disabled.");
            }

            return OfflineIdentity.VerifyOffline(envelope, clientBytes);
        }

        return LoginIdentity.Verify(identityBytes);
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

    private static CompoundTag? LoadPlayerDataCompat(
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
                    server.PlayerData.Save(primaryXuid, pending);
                    world.Provider.DeletePlayerData(candidate);
                    return pending;
                }

                byte[]? raw = provider.GetRawPlayerData(candidate);
                if (raw is not null && provider.LoadPlayerDataFromRaw(raw) is { } data) {
                    server.PlayerData.Save(primaryXuid, data);
                    provider.DeletePlayerData(candidate);
                    return data;
                }

            }
        }

        return null;
    }

    private static UUID FromGuid(Guid guid) {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        return new UUID {
            MostSignificantBits = System.Buffers.Binary.BinaryPrimitives
                .ReadUInt64BigEndian(bytes[..8]),

            LeastSignificantBits = System.Buffers.Binary.BinaryPrimitives
                .ReadUInt64BigEndian(bytes[8..])
        };
    }

    public static SerializedSkin FromClientData(ClientData data) {
        static byte[] DecodeBase64(string value) {
            if (string.IsNullOrEmpty(value)) {
                return [];
            }

            string normalized = value
                .Replace('-', '+')
                .Replace('_', '/');

            int remainder = normalized.Length & 3;
            if (remainder != 0) {
                normalized = normalized.PadRight(
                    normalized.Length + 4 - remainder,
                    '='
                );
            }

            try {
                return Convert.FromBase64String(normalized);
            }
            catch (FormatException) {
                return [];
            }
        }

        static string DecodeString(string value) {
            byte[] bytes = DecodeBase64(value);

            return bytes.Length == 0
                ? string.Empty
                : System.Text.Encoding.UTF8.GetString(bytes);
        }

        static SkinImage DecodeImage(string value, uint width, uint height) {
            byte[] bytes = DecodeBase64(value);

            if ((ulong)bytes.Length != (ulong)width * height * 4) {
                return new SkinImage {
                    Width = 0,
                    Height = 0,
                    ImageBytes = []
                };
            }

            return new SkinImage {
                Width = width,
                Height = height,
                ImageBytes = [.. bytes]
            };
        }

        static PieceType ParsePieceType(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return PieceType.Skeleton;
            }

            ReadOnlySpan<char> span = value.AsSpan();

            if (span.StartsWith("persona_", StringComparison.OrdinalIgnoreCase)) {
                span = span[8..];
            }

            return Enum.TryParse(
                span,
                true,
                out PieceType result
            )
                ? result
                : PieceType.Skeleton;
        }

        static UUID ParseUuid(string value) {
            if (!Guid.TryParse(value, out Guid guid)) {
                return new UUID();
            }

            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes, bigEndian: true, out _);

            return new UUID {
                MostSignificantBits =
                    System.Buffers.Binary.BinaryPrimitives
                        .ReadUInt64BigEndian(bytes),

                LeastSignificantBits =
                    System.Buffers.Binary.BinaryPrimitives
                        .ReadUInt64BigEndian(bytes[8..])
            };
        }

        static Color ParseColor(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return new Color();
            }

            ReadOnlySpan<char> span = value.AsSpan().Trim();

            if (!span.IsEmpty && span[0] == '#') {
                span = span[1..];
            }

            return uint.TryParse(
                span,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out uint color
            )
                ? new Color {
                    Value = unchecked((int)color)
                }
                : new Color();
        }

        var animations = new List<AnimatedImageData>(
            data.AnimatedImageData.Length
        );

        foreach (SkinAnimation animation in data.AnimatedImageData) {
            animations.Add(new AnimatedImageData {
                SkinImage = DecodeImage(
                    animation.Image,
                    animation.ImageWidth,
                    animation.ImageHeight
                ),

                AnimatedTextureType =
                    (AnimatedTextureType)animation.Type,

                Frames = animation.Frames,

                AnimationExpression =
                    (AnimationExpression)animation.AnimationExpression
            });
        }

        var pieces = new List<SerializedPersonaPieceHandle>(
            data.PersonaPieces.Length
        );

        foreach (PersonaPiece piece in data.PersonaPieces) {
            pieces.Add(new SerializedPersonaPieceHandle {
                PieceId = piece.PieceId,
                PieceType = ParsePieceType(piece.PieceType),
                PackId = ParseUuid(piece.PackId),
                IsDefaultPiece = piece.IsDefault,
                ProductId = piece.ProductId
            });
        }

        var tints = new Dictionary<string, TintMapColor>(
            data.PieceTintColors.Length
        );

        foreach (TintPiece tint in data.PieceTintColors) {
            string pieceType = tint.PieceType.StartsWith("persona_", StringComparison.Ordinal)
                ? tint.PieceType[8..]
                : tint.PieceType;
            if (pieceType == "hand") {
                pieceType = "hands";
            }

            tints[pieceType] = new TintMapColor {
                Colors = [.. tint.Colors.Select(ParseColor)]
            };
        }
        return new SerializedSkin {
            ID = data.SkinId,
            FullID = data.SkinId,
            PlayFabID = data.PlayFabId,

            ProfileHash = string.Empty,

            ResourcePatch = DecodeString(data.SkinResourcePatch),

            ImageData = DecodeImage(
                data.SkinData,
                data.SkinImageWidth,
                data.SkinImageHeight
            ),

            CapeImageData = DecodeImage(
                data.CapeData,
                data.CapeImageWidth,
                data.CapeImageHeight
            ),

            AnimatedImageData = animations,

            GeometryData =
                DecodeString(data.SkinGeometryData),

            GeometryDataMinEngineVersion =
                DecodeString(data.SkinGeometryDataEngineVersion),

            AnimationData =
                DecodeString(data.SkinAnimationData),

            CapeID = data.CapeId,

            ArmSize = default,

            SkinColor =
                ParseColor(data.SkinColor),

            PersonaPieces = pieces,
            PieceTintColors = tints,

            IsPremium =
                data.PremiumSkin,

            IsPersona =
                data.PersonaSkin,

            IsPersonaCapeOnClassicSkin =
                data.CapeOnClassicSkin,

            IsPrimaryUser = true,

            OverridesPlayerAppearance =
                data.OverrideSkin,

            TrustedSkinFlag =
                (TrustedSkinFlag)(data.TrustedSkin ? 1 : 0)
        };
    }
}
