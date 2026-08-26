namespace Basalt.Core.Network.Handlers;

using Basalt.Binary;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;

using Basalt.BedrockProtocol;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.NBT;

using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;
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

    public ResourcePackCompletedTask(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        Dimension? dimension) {
        _server = server;
        _connection = connection;
        _player = player;
        _dimension = dimension;
        CompletionMailbox = dimension?.Mailbox;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ResourcePackCompleted.Execute") : default;

        _playerListPackets = _server.CurrentPlayersSnapshot.Select(static online => new PlayerListPacket {
            Action = PlayerListPacketType.Add,
            AddEntries = [CreatePlayerListEntry(online)]
        }).ToList();

        _broadcastEntry = new PlayerListPacket {
            Action = PlayerListPacketType.Add,
            AddEntries = [CreatePlayerListEntry(_player)]
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
            EntityId = _player.UniqueId,
            RuntimeId = (ulong)_player.UniqueId,
            GameType = _player.GetGamemode(),
            BlockNetworkIdsAreHashes = true,
            EnableItemStackNetManager = true,
            EnchantmentSeed = 0,
            Trial = false,
            LevelCurrentTime = (ulong)(_dimension?.World?.DayTime ?? 0),
            LevelId = "",
            LevelName = "Basalt",
            MovementSettings = new SyncedPlayerMovementSettings() {
                RewindHistorySize = 0,
                ServerAuthoritativeBlockBreaking = true,
            },
            MultiplayerCorrelationId = Guid.NewGuid().ToString(),
            NetworkPermissions = new NetworkPermissions() {
                ServerAuthSoundEnabled = false,
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
            ServerVersion = "1.26.50",
            Settings = new LevelSettings() {
                AchievementsDisabled = !_server.Properties.AchievementsEnabled,
                AllowAnonymousBlockDropsInEditorWorlds = false,
                BaseGameVersion = "1.26.50",
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
                EducationFeaturesEnabled = false,
                EducationProductId = "",
                EduSharedUriResource = new EduSharedUriResource() {
                    ButtonName = "",
                    LinkUri = "",
                },
                EmoteChatMuted = false,
                Experiments = new Experiments() {
                    ExperimentsEverToggled = false,
                    Toggles = [],
                },
                GameDifficulty = Difficulty.Normal,
                GeneratorType = GeneratorType.Overworld,
                GameType = _player.GetGamemode(),
                BonusChestEnabled = false,
                ConfirmedPlatformLockedContent = false,
                LockedBehaviorPack = false,
                LockedResourcePack = false,
                CreatedInEditor = false,
                ExportedFromEditor = false,
                FromLockedTemplate = false,
                FromWorldTemplate = false,
                Hardcore = false,
                WorldTemplateOptionLocked = false,
                LanBroadcastIntent = true,
                LightningLevel = 0,
                LimitedWorldDepth = 0,
                LimitedWorldWidth = 0,
                MultiplayerGameIntent = true,
                NetherType = true,
                OnlySpawnV1Villagers = false,
                OverrideForceExperimentalGameplay = null,
                PersonaDisabled = false,
                PlatformBroadcastSetting = GamePublishSetting.Public,
                PlayerPermissions = _player.IsOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member,
                RainLevel = 0,
                Rules = new() {
                    Rules = [
                        new GameRule() {
                            Name = "naturalregeneration",
                            ValueType = GameRuleValueType.Bool,
                            BoolValue = false,
                        },
                        new GameRule() {
                            Name = "locatorBar",
                            ValueType = GameRuleValueType.Bool,
                            BoolValue = false,
                        },
                    ],
                },
                Seed = 0,
                ServerChunkTickRange = _server.Properties.SimulationDistance,
                ServerEditorConnectionPolicy = (int)ServerEditorConnectionPolicy.MatchWorldType,
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
            WorldTemplateId = new Uuid {
                MostSignificantBits = 0,
                LeastSignificantBits = 0
            },
            TemplateContentIdentity = "",
            PlayerPropertyData = new CompoundTag(),
                BlockProperties = BlockPalette.GetCustomBlockEntries().ToArray(),
        };

        _startGamePayload = SerializePacket(startGame);

        PlayStatusPacket spawnStatus = new PlayStatusPacket() {
            Status = PlayStatus.PlayerSpawn,
        };
        _spawnStatusPayload = SerializePacket(spawnStatus);
    }

    private static byte[] SerializePacket(Packet packet) {
        using BinaryStream stream = BinaryStream.Rent(1024 * 1024);
        BinaryWriter writer = stream;
        packet.Serialize(ref writer);
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

        AvailableCommandsPacket availableCommands = _server.Commands.BuildAvailableCommandsPacket(_player);

        _server.Network.SendSerializedPackets(_connection, [
            (GetPacketId<JigsawStructureDataPacket>(), SerializePacket(new JigsawStructureDataPacket {
                StructureData = CreateJigsawStructureData(),
            })),
            (GetPacketId<VoxelShapesPacket>(), SerializePacket(new VoxelShapesPacket())),
            (GetPacketId<StartGamePacket>(), _startGamePayload!),
            (GetPacketId<ItemRegistryPacket>(), ItemPalette.GetItemRegistryPayload()),
            (GetPacketId<AvailableActorIdentifiersPacket>(), EntityPalette.GetActorIdentifiersPayload()),
            (GetPacketId<PlayStatusPacket>(), _spawnStatusPayload!),
            (GetPacketId<SetCommandsEnabledPacket>(), SerializePacket(new SetCommandsEnabledPacket {
                Enabled = !_server.Properties.AchievementsEnabled,
            })),
            (GetPacketId<CreativeContentPacket>(), ItemPalette.GetCreativeContentPayload()),
            (GetPacketId<CraftingDataPacket>(), Crafting.CraftingRegistry.Instance.GetCraftingDataPayload())
        ]);

        _server.Network.QueuePackets(_connection, _playerListPackets); // An error occured 
        _server.Broadcast(_broadcastEntry!, _player);
        _server.Network.QueuePacket(
            _connection,
            _player.Abilities.CreatePacket(_player.UniqueId, _player.IsOperator)
        );
        _server.Network.QueuePacket(_connection, availableCommands);
    }

    private static CompoundTag CreateJigsawStructureData() {
        CompoundTag structureData = new();
        structureData.Set("processors", new ListTag());
        structureData.Set("template_pools", new ListTag());
        structureData.Set("jigsaws", new ListTag());
        structureData.Set("structure_sets", new ListTag());
        return structureData;
    }

    private static int GetPacketId<TPacket>() where TPacket : Packet {
        return typeof(TPacket).GetCustomAttributes(typeof(PacketIdAttribute), false)
            .Cast<PacketIdAttribute>()
            .Single().Id;
    }

    private static PlayerListAddEntry CreatePlayerListEntry(Player.Player player) => new() {
        Uuid = FromGuid(player.Uuid),
        ActorUniqueId = player.UniqueId,
        PlayerName = player.Username,
        Xuid = player.Xuid,
        PlatformOnlineId = "",
        BuildPlatform = (int)player.DeviceOS,
        Skin = player.Skin,
        Teacher = false,
        Host = false,
        SubClient = false,
        PlayerColor = 0,
    };

    private static Uuid FromGuid(Guid guid) {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);
        return new Uuid {
            MostSignificantBits = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]),
            LeastSignificantBits = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes[8..])
        };
    }

    internal static Dimension? ResolvePlayerDimension(Server server, Player.Player player) {
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
