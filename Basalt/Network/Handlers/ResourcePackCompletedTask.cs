namespace Basalt.Core.Network.Handlers;

using Basalt.Binary;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.RakNet;

using BedrockProtocol.Enums;
using BedrockProtocol.Types;
using BedrockProtocol.Packets;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;
using BedrockProtocol.Nbt;
using Basalt.Core.Blocks;
using Basalt.Core.Item;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;

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
    private Vec3 _playerPosition = new();

    public ResourcePackCompletedTask(Server server, NetworkConnection connection, Player.Player player) {
        _server = server;
        _connection = connection;
        _player = player;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ResourcePackCompleted.Execute") : default;

        _dimension = ResolvePlayerDimension(_server, _player);

        _playerListPackets = _server.Players.Values.Select(static online => new PlayerListPacket {
            Entries = [online.CreatePlayerListEntry()]
        }).ToList();

        _broadcastEntry = new PlayerListPacket {
            Entries = new List<PlayerListEntryVariant> {
                _player.CreatePlayerListEntry(),
            }
        };

        Vec3 spawnPosition = _dimension?.SpawnPosition ?? new Vec3 { X = 0f, Y = -57f, Z = 0f };
        _playerPosition = new Vec3 {
            X = spawnPosition.X,
            Y = spawnPosition.Y + EntityCollisionTrait.DefaultHeight,
            Z = spawnPosition.Z
        };
        int dimensionId = 0;

        if (_dimension is not null) {
            bool savedDimension =
                string.Equals(
                    _dimension.World?.Name,
                    _player.SavedWorldName,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    _dimension.Identifier,
                    _player.SavedDimensionIdentifier,
                    StringComparison.OrdinalIgnoreCase);
            if (savedDimension) {
                _playerPosition = _player.Position;
            }
            dimensionId = (int)_dimension.Type;
        }

        StartGamePacket startGame = new() {
            EntityID = new() {
                Value = _player.UniqueId,
            },
            RuntimeID = new() {
                Value = (ulong)_player.UniqueId,
            },
            GameType = _player.GetGamemode(),
            BlockNetworkIdsAreHashes = true,
            EnableItemStackNetManager = true,
            EnchantmentSeed = 0,
            IsTrial = false,
            LevelCurrentTime = 0,
            LevelID = "",
            LevelName = "Basalt",
            MovementSettings = new SyncedPlayerMovementSettings() {
                RewindHistorySize = 0,
                ServerAuthoritativeBlockBreaking = true,
            },
            MultiplayerCorrelationId = Guid.NewGuid().ToString(),
            NetworkPermissions = new NetworkPermissions() {
                ServerAuthSoundEnabled = true,
            },
            Position = new Vec3() {
                X = _playerPosition.X,
                Y = _playerPosition.Y,
                Z = _playerPosition.Z,
            },
            Rotation = new Vec2() {
                X = 0f,
                Y = 0f,
            },
            ServerBlockTypeRegistryChecksum = 0,
            ServerConfigurationJoinInfo = null,
            ServerEnabledClientSideGeneration = false,
            ServerTelemetryData = new ServerTelemetryData() {
                OwnerId = "Basalt",
                ScenarioId = "",
                ServerId = "",
                WorldId = "",
            },
            ServerVersion = "1.21.40",
            Settings = new LevelSettings() {
                AchievementsDisabled = !_server.Properties.AchievementsEnabled,
                AllowAnonymousBlockDropsInEditorWorlds = true,
                BaseGameVersion = "1.21.40",
                ChatRestrictionLevel = ChatRestrictionLevel.None,
                CommandsEnabled = !_server.Properties.AchievementsEnabled,
                CustomSkinsDisabled = false,
                DayCycleStopTime = 0,
                DefaultSpawnBlockPosition = new BlockPos() {
                    X = (int)(_dimension?.SpawnPosition.X ?? 0),
                    Y = (int)(_dimension?.SpawnPosition.Y ?? 0),
                    Z = (int)(_dimension?.SpawnPosition.Z ?? 0)
                },
                DisablePlayerInteractions = false,
                EditorWorldType = EditorWorldType.NonEditor,
                EducationEditionOffer = EducationEditionOffer.None,
                EducationFeaturesEnabled = true,
                EducationProductID = "",
                EduSharedUriResource = new EduSharedUriResource() {
                    ButtonName = "",
                    LinkUri = "",
                },
                EmoteChatMuted = true,
                Experiments = new Experiments() {
                    ExperimentsEverToggled = false,
                    Toggles = [],
                },
                GameDifficulty = Difficulty.Normal,
                GeneratorType = GeneratorType.Overworld,
                GameType = _player.GetGamemode(),
                HasBonusChestEnabled = false,
                HasConfirmedPlatformLockedContent = false,
                HasLockedBehaviorPack = false,
                HasLockedResourcePack = false,
                IsCreatedInEditor = false,
                IsExportedFromEditor = false,
                IsFromLockedTemplate = false,
                IsFromWorldTemplate = false,
                IsHardcore = false,
                IsWorldTemplateOptionLocked = false,
                LANBroadcastIntent = true,
                LightningLevel = 0,
                LimitedWorldDepth = 0,
                LimitedWorldWidth = 0,
                MultiplayerGameIntent = true,
                NetherType = true,
                OnlySpawnV1Villagers = false,
                OverrideForceExperimentalGameplay = false,
                PersonaDisabled = false,
                PlatformBroadcastSetting = GamePublishSetting.Public,
                PlayerPermissions = _player.IsOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member,
                RainLevel = 0,
                RuleData = new() {
                    RulesList = [],
                },
                Seed = 0,
                ServerChunkTickRange = _server.Properties.SimulationDistance,
                ServerEditorConnectionPolicy = ServerEditorConnectionPolicy.Mixed,
                SpawnSettings = new SpawnSettings() {
                    Dimension = dimensionId,
                    SpawnBiomeType = SpawnBiomeType.Default,
                    UserDefinedBiomeName = "",
                },
                StartWithMapEnabled = false,
                TexturePacksRequired = _server.Properties.ForceResourcePacks,
                UseMsaGamertagsOnly = false,
                XboxLiveBroadcastSetting = GamePublishSetting.Public,
            },
            WorldTemplateID = new UUID {
                MostSignificantBits = 0,
                LeastSignificantBits = 0
            },
            TemplateContentIdentity = "",
            PlayerPropertyData = new CompoundTag(),
            BlockProperties = BlockPalette.GetCustomBlockEntries(),
        };

        Logger.Info($"Spawn IDs: unique={_player.UniqueId}, runtime={_player.RuntimeId}, startGameEntity={startGame.EntityID.Value}, startGameRuntime={startGame.RuntimeID.Value}");




        _startGamePayload = SerializePacket(startGame);

        PlayStatusPacket spawnStatus = new PlayStatusPacket() {
            Status = PlayStatus.PlayerSpawn,
        };
        _spawnStatusPayload = SerializePacket(spawnStatus);
    }

    private static byte[] SerializePacket(Packet packet) {
        using BinaryStream stream = BinaryStream.Rent(1024 * 1024);
        BinaryWriter writer = stream;
        packet.Serialize(writer);
        return writer.GetProcessedBytes().ToArray();
    }

    public override void Complete() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ResourcePackCompleted.Complete") : default;

        if (_dimension is not null) {
            _player.Position = _playerPosition;

            EntitySpawnOptions options = new(InitialSpawn: true);
            PlayerSpawnSignal spawnSignal = new(_player, options);
            _server.Emit(spawnSignal);
            if (!spawnSignal.Emit()) {
                DisconnectPacket forcedDisconnect = new() {
                    // Reason = DisconnectReason.Disconnected,
                    // HideDisconnectionScreen = false,
                    // Message = "Server force closed the connection.",
                    // FilteredMessage = "Server force closed the connection."
                    Reason = DisconnectFailReason.UnrecoverableError,
                    Messages = new DisconnectPacketMessages() {
                        Message = "Server force closed the connection.",
                        FilteredMessage = "Server force closed the connection.",
                    },
                };
                _server.Network.QueuePacket(_connection, forcedDisconnect);
                _connection.Disconnect();
                return;
            }

            _player.Spawn(_dimension, spawnSignal.Options);
        }

        _server.Network.SendSerializedPackets(_connection, [
            (StartGamePacket.PacketId, _startGamePayload!),
            (ItemRegistryPacket.PacketId, ItemPalette.GetItemRegistryPayload()),
            (AvailableActorIdentifiersPacket.PacketId, EntityPalette.GetActorIdentifiersPayload()),
            (PlayStatusPacket.PacketId, _spawnStatusPayload!),
            (CreativeContentPacket.PacketId, ItemPalette.GetCreativeContentPayload()),
            (CraftingDataPacket.PacketId, Crafting.CraftingRegistry.Instance.GetCraftingDataPayload())
        ]);

        _server.Network.QueuePackets(_connection, _playerListPackets); // An error occured 
        _server.Broadcast(_broadcastEntry!, _player);
        _player.Permissions.Sync();
    }

    private static Dimension? ResolvePlayerDimension(Server server, Player.Player player) {
        if (player.SavedWorldName is not null && player.SavedDimensionIdentifier is not null) {
            Worlds.World? world = server.Worlds.FirstOrDefault(world =>
                string.Equals(world.Name, player.SavedWorldName, StringComparison.OrdinalIgnoreCase));

            if (world is null) {
                string worldPath = Path.Combine(server.Properties.WorldPath, player.SavedWorldName);
                if (Directory.Exists(worldPath)) {
                    try {
                        world = server.LoadWorld(
                            player.SavedWorldName,
                            server.Properties.WorldProvider,
                            worldPath);
                    }
                    catch {
                        world = null;
                    }
                }
            }

            if (world is not null) {
                if (world.DimensionCount == 0) {
                    world.CreateDimension(
                        player.SavedDimensionIdentifier,
                        DimensionId.Overworld,
                        typeof(VoidGenerator));
                }

                Dimension? dimension = world.GetDimension(player.SavedDimensionIdentifier);
                if (dimension is not null) {
                    return dimension;
                }
            }
        }

        return server.GetWorld().GetDimension(DimensionId.Overworld);
    }

}
