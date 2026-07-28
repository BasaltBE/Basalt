namespace Basalt.Core.Network.Handlers;

using Basalt.Binary;
using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

internal sealed class ResourcePackCompletedTask : ServerTask {
    private readonly Server _server;
    private readonly NetworkConnection _connection;
    private readonly Player.Player _player;

    // Built on worker thread.
    private List<PlayerListPacket> _playerListPackets = [];
    private PlayerListPacket? _broadcastEntry;
    private byte[]? _startGamePayload;
    private byte[]? _spawnStatusPayload;
    private Worlds.Dimensions.Dimension? _dimension;
    private Vec3f _playerPosition;

    public ResourcePackCompletedTask(Server server, NetworkConnection connection, Player.Player player) {
        _server = server;
        _connection = connection;
        _player = player;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ResourcePackCompleted.Execute") : default;

        _dimension = ResolvePlayerDimension(_server, _player);

        _playerListPackets = _server.Players.Values.Select(static online => new PlayerListPacket {
            ActionType = PlayerListActionType.Add,
            Entries = [online.CreatePlayerListEntry()]
        }).ToList();

        _broadcastEntry = new PlayerListPacket {
            ActionType = PlayerListActionType.Add,
            Entries = [_player.CreatePlayerListEntry()]
        };

        _playerPosition = _dimension?.SpawnPosition ?? new Vec3f { X = 0f, Y = -57f, Z = 0f };
        int dimensionId = 0;

        if (_dimension is not null) {
            if (_player.SavedWorldName is not null) {
                _playerPosition = _player.Location;
            }
            dimensionId = (int)_dimension.Type;
        }

        StartGamePacket startGame = new() {
            EntityUniqueId = _player.UniqueId,
            EntityRuntimeId = _player.RuntimeId,
            PlayerGameMode = (int)_player.GetGamemode(),
            PlayerPosition = _playerPosition,
            Pitch = 0f,
            Yaw = 0f,
            WorldSeed = 0,
            SpawnBiomeType = SpawnBiomeType.Default,
            UserDefinedBiomeName = "plains",
            Dimension = dimensionId,
            Generator = 1,
            WorldGameMode = 0,
            Hardcore = false,
            Difficulty = 1,
            WorldSpawn = new BlockPos {
                X = (int)(_dimension?.SpawnPosition.X ?? 0),
                Y = (int)(_dimension?.SpawnPosition.Y ?? -58),
                Z = (int)(_dimension?.SpawnPosition.Z ?? 0)
            },
            AchievementsDisabled = !_server.Properties.AchievementsEnabled,
            EditorWorldType = EditorWorldType.NotEditor,
            CreatedInEditor = false,
            ExportedFromEditor = false,
            DayCycleLockTime = 0,
            EducationEditionOffer = 0,
            EducationFeaturesEnabled = false,
            EducationProductId = string.Empty,
            RainLevel = 0f,
            LightningLevel = 0f,
            ConfirmedPlatformLockedContent = false,
            MultiPlayerGame = true,
            LanBroadcastEnabled = false,
            XblBroadcastMode = XblBroadcastMode.Public,
            PlatformBroadcastMode = (int)XblBroadcastMode.Public,
            CommandsEnabled = !_server.Properties.AchievementsEnabled,
            TexturePackRequired = false,
            GameRules = [],
            Experiments = [],
            ExperimentsPreviouslyToggled = false,
            BonusChestEnabled = false,
            StartWithMapEnabled = false,
            PlayerPermissions = _player.IsOperator ? 2 : 1,
            ServerChunkTickRadius = 4,
            HasLockedBehaviourPack = false,
            HasLockedTexturePack = false,
            FromLockedWorldTemplate = false,
            MsaGamerTagsOnly = false,
            FromWorldTemplate = false,
            WorldTemplateSettingsLocked = false,
            OnlySpawnV1Villagers = false,
            PersonaDisabled = false,
            CustomSkinsDisabled = false,
            EmoteChatMuted = false,
            BaseGameVersion = Constants.MinecraftVersion,
            LimitedWorldWidth = 0,
            LimitedWorldDepth = 0,
            NewNether = true,
            EducationSharedResourceUri = new EducationSharedResourceUri {
                ButtonName = string.Empty,
                LinkUri = string.Empty
            },
            ForceExperimentalGameplay = new Optional<BoolType> { HasValue = false },
            ChatRestrictionLevel = ChatRestrictionLevel.None,
            DisablePlayerInteractions = false,
            LevelId = "BasaltWorld",
            WorldName = "Basalt",
            TemplateContentIdentity = string.Empty,
            Trial = false,
            PlayerMovementSettings = new PlayerMovementSettings {
                RewindHistorySize = 0,
                ServerAuthoritativeBlockBreaking = true
            },
            Time = 0,
            EnchantmentSeed = 0,
            Blocks = BlockPalette.GetCustomBlockEntries(),
            MultiPlayerCorrelationId = Guid.NewGuid().ToString(),
            ServerAuthoritativeInventory = true,
            GameVersion = Constants.MinecraftVersion,
            PropertyData = new CompoundTag(),
            ServerBlockStateChecksum = 0,
            WorldTemplateId = Guid.Empty,
            ClientSideGeneration = false,
            UseBlockNetworkIdHashes = true,
            ServerAuthoritativeSound = true,
            ServerJoinInformation = new OptionalValue<ServerJoinInformation> { HasValue = false },
            ServerId = string.Empty,
            ScenarioId = string.Empty,
            WorldId = string.Empty,
            OwnerId = _player.Xuid
        };

        _startGamePayload = SerializePacket(startGame);

        PlayStatusPacket spawnStatus = new(PlayStatus.PlayerSpawn);
        _spawnStatusPayload = SerializePacket(spawnStatus);
    }

    private static byte[] SerializePacket(DataPacket packet) {
        using BinaryStream stream = BinaryStream.Rent(1024 * 1024);
        BinaryWriter writer = stream;
        packet.Serialize(writer);
        return writer.GetProcessedBytes().ToArray();
    }

    public override void Complete() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ResourcePackCompleted.Complete") : default;

        if (_dimension is not null) {
            if (_player.SavedWorldName is null) {
                _player.Location = _playerPosition;
            }

            EntitySpawnOptions options = new(InitialSpawn: true);
            PlayerSpawnSignal spawnSignal = new(_player, options);
            _server.Emit(spawnSignal);
            if (!spawnSignal.Emit()) {
                DisconnectPacket forcedDisconnect = new() {
                    Reason = DisconnectReason.Disconnected,
                    HideDisconnectionScreen = false,
                    Message = "Server force closed the connection.",
                    FilteredMessage = "Server force closed the connection."
                };
                _server.Network.QueuePacket(_connection, forcedDisconnect);
                _connection.Disconnect();
                return;
            }

            _player.Spawn(_dimension, spawnSignal.Options);
        }

        _server.Network.SendSerializedPackets(_connection, [
            (PacketId.StartGame, _startGamePayload!),
            (PacketId.ItemRegistry, ItemPalette.GetItemRegistryPayload()),
            (PacketId.AvailableActorIdentifiers, EntityPalette.GetActorIdentifiersPayload()),
            (PacketId.PlayStatus, _spawnStatusPayload!),
            (PacketId.CreativeContent, ItemPalette.GetCreativeContentPayload()),
            (PacketId.CraftingData, Crafting.CraftingRegistry.Instance.GetCraftingDataPayload())
        ]);
        _server.Network.QueuePackets(_connection, _playerListPackets);
        _server.Broadcast(_broadcastEntry!, _player);
        _player.Permissions.Sync();
    }

    private static Worlds.Dimensions.Dimension? ResolvePlayerDimension(Server server, Player.Player player) {
        if (player.SavedWorldName is not null && player.SavedDimensionIdentifier is not null) {
            foreach (Worlds.World world in server.Worlds) {
                if (string.Equals(world.Name, player.SavedWorldName, StringComparison.OrdinalIgnoreCase)) {
                    Worlds.Dimensions.Dimension? dim = world.GetDimension(player.SavedDimensionIdentifier);
                    if (dim is not null) {
                        return dim;
                    }
                }
            }
        }

        return server.GetWorld().GetDimension(DimensionType.Overworld);
    }
}
