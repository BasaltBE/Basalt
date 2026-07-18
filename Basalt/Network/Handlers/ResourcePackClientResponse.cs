namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Profiling;
using Basalt.Core.Resources;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Protocol.Io;
using Basalt.Core.Blocks;


public static class ResourcePackClientResponse
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        using var __zone = Profiler.BeginZone("ResourcePackResponse.Handle");
        ResourcePackClientResponsePacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ResourcePackClientResponsePacket)Protocol.Io.Packet.Deserialize(reader);

        switch (packet.Response)
        {
            case ResourcePackResponse.Refused:
                if (server.Properties.ForceResourcePacks)
                {
                    DisconnectPacket disconnect = new()
                    {
                        Reason = DisconnectReason.ResourcePackProblem,
                        HideDisconnectionScreen = false,
                        Message = "Required resource packs were refused.",
                        FilteredMessage = "Required resource packs were refused."
                    };
                    server.Network.SendPacket(connection, disconnect);
                }
                return;

            case ResourcePackResponse.SendPacks:
                foreach (string packId in packet.PacksToDownload)
                {
                    ResourcePack? pack = server.ResourcePacks.GetByUuid(packId);
                    if (pack is null)
                    {
                        Logger.Warn($"Client requested unknown pack: {packId}");
                        continue;
                    }

                    uint chunkSize = server.ResourcePacks.ChunkSize;
                    ResourcePackDataInfoPacket dataInfo = new()
                    {
                        Uuid = pack.Uuid.ToString(),
                        ChunkSize = chunkSize,
                        ChunkCount = pack.ChunkCount(chunkSize),
                        Size = pack.Size,
                        Hash = pack.Hash,
                        Premium = false,
                        PackType = 6
                    };
                    server.Network.SendPacket(connection, dataInfo);
                }
                return;

            case ResourcePackResponse.AllPacksDownloaded:
                List<ResourcePackStackEntry> stackPacks =
                [
                    new ResourcePackStackEntry
                    {
                        Uuid = Guid.Parse("0fba4063-dba1-4281-9b89-ff9390653530"),
                        Version = "1.0.0",
                        SubPackName = ""
                    }
                ];

                foreach (ResourcePack loadedPack in server.ResourcePacks.Packs)
                {
                    stackPacks.Add(new ResourcePackStackEntry
                    {
                        Uuid = loadedPack.Uuid,
                        Version = loadedPack.VersionString,
                        SubPackName = "Education Edition Resource Pack"
                    });
                }

                ResourcePackStackPacket stack = new()
                {
                    MustAccept = server.Properties.ForceResourcePacks,
                    Packs = stackPacks,
                    BaseGameVersion = Constants.MinecraftVersion,
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    IncludeEditorPacks = true
                };
                server.Network.SendPacket(connection, stack);
                return;

            case ResourcePackResponse.Completed:
                if (!server.Players.TryGetValue(connection, out Player.Player? player))
                {
                    Console.WriteLine("Resource pack flow completed, but no player session was found.");
                    DisconnectPacket missingSessionDisconnect = new()
                    {
                        Reason = DisconnectReason.Disconnected,
                        HideDisconnectionScreen = false,
                        Message = "Server force closed the connection.",
                        FilteredMessage = "Server force closed the connection."
                    };
                    server.Network.SendPacket(connection, missingSessionDisconnect);
                    connection.Disconnect();
                    return;
                }

                PlayerListPacket playerList = new()
                {
                    ActionType = PlayerListActionType.Add,
                    Entries = server.Players.Values.Select(static online => online.CreatePlayerListEntry()).ToList()
                };
                server.Network.SendPacket(connection, playerList);
                server.Broadcast(new PlayerListPacket
                {
                    ActionType = PlayerListActionType.Add,
                    Entries = [player.CreatePlayerListEntry()]
                }, player);

                StartGamePacket startGame = new()
                {
                    EntityUniqueId = player.UniqueId,
                    EntityRuntimeId = player.RuntimeId,
                    PlayerGameMode = (int)player.GetGamemode(),
                    PlayerPosition = new Vec3f { X = 0f, Y = -57f, Z = 0f },
                    Pitch = 0f,
                    Yaw = 0f,
                    WorldSeed = 0,
                    SpawnBiomeType = SpawnBiomeType.Default,
                    UserDefinedBiomeName = "plains",
                    Dimension = 0,
                    Generator = 1,
                    WorldGameMode = 0,
                    Hardcore = false,
                    Difficulty = 1,
                    WorldSpawn = new BlockPos { X = 0, Y = -58, Z = 0 },
                    AchievementsDisabled = !server.Properties.AchievementsEnabled,
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
                    CommandsEnabled = !server.Properties.AchievementsEnabled,
                    TexturePackRequired = false,
                    GameRules = [],
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    BonusChestEnabled = false,
                    StartWithMapEnabled = false,
                    PlayerPermissions = player.IsOperator ? 2 : 1,
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
                    EducationSharedResourceUri = new EducationSharedResourceUri
                    {
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
                    PlayerMovementSettings = new PlayerMovementSettings
                    {
                        RewindHistorySize = 0,
                        ServerAuthoritativeBlockBreaking = true
                    },
                    Time = 0,
                    EnchantmentSeed = 0,
                    Blocks = BlockPalette.GetCustomBlockEntries(),
                    MultiPlayerCorrelationId = Guid.NewGuid().ToString(),
                    ServerAuthoritativeInventory = true,
                    GameVersion = Constants.MinecraftVersion,
                    PropertyData = new Protocol.Nbt.CompoundTag(),
                    ServerBlockStateChecksum = 0,
                    WorldTemplateId = Guid.Empty,
                    ClientSideGeneration = false,
                    UseBlockNetworkIdHashes = true,
                    ServerAuthoritativeSound = true,
                    ServerJoinInformation = new OptionalValue<ServerJoinInformation> { HasValue = false },
                    ServerId = string.Empty,
                    ScenarioId = string.Empty,
                    WorldId = string.Empty,
                    OwnerId = player.Xuid
                };
                var dimension = ResolvePlayerDimension(server, player);
                if (dimension is not null)
                {
                    if (player.SavedWorldName is not null)
                    {
                        startGame.PlayerPosition = player.Location;
                    }
                    else
                    {
                        player.Location = startGame.PlayerPosition;
                    }

                    startGame.Dimension = (int)dimension.Type;

                    EntitySpawnOptions options = new(InitialSpawn: true);
                    PlayerSpawnSignal spawnSignal = new(player, options);
                    server.Emit(spawnSignal);
                    if (!spawnSignal.Emit())
                    {
                        DisconnectPacket forcedDisconnect = new()
                        {
                            Reason = DisconnectReason.Disconnected,
                            HideDisconnectionScreen = false,
                            Message = "Server force closed the connection.",
                            FilteredMessage = "Server force closed the connection."
                        };
                        server.Network.SendPacket(connection, forcedDisconnect);
                        connection.Disconnect();
                        return;
                    }

                    player.Spawn(dimension, spawnSignal.Options);
                }

                byte[] itemRegistryPayload = ItemPalette.GetItemRegistryPayload();
                byte[] creativeContentPayload = ItemPalette.GetCreativeContentPayload();
                AvailableActorIdentifiersPacket actorIdentifiers = new()
                {
                    Data = EntityPalette.BuildAvailableActorIdentifiersTag()
                };

                PlayStatusPacket spawnStatus = new(PlayStatus.PlayerSpawn);

                server.Network.SendPackets(connection, [startGame]);
                player.Permissions.Sync();
                server.Network.SendSerializedPacket(connection, PacketId.ItemRegistry, itemRegistryPayload);
                // server.Network.SendPackets(connection, [spawnStatus]);
                server.Network.SendPackets(connection, [actorIdentifiers, spawnStatus]);
                server.Network.SendSerializedPacket(connection, PacketId.CreativeContent, creativeContentPayload);
                server.Network.SendSerializedPacket(connection, PacketId.CraftingData, Crafting.CraftingRegistry.Instance.GetCraftingDataPayload());
                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {(byte)packet.Response}");
                return;
        }
    }

    private static Worlds.Dimensions.Dimension? ResolvePlayerDimension(Server server, Player.Player player)
    {
        if (player.SavedWorldName is not null && player.SavedDimensionIdentifier is not null)
        {
            foreach (var world in server.Worlds)
            {
                if (string.Equals(world.Name, player.SavedWorldName, StringComparison.OrdinalIgnoreCase))
                {
                    var dim = world.GetDimension(player.SavedDimensionIdentifier);
                    if (dim is not null)
                    {
                        return dim;
                    }
                }
            }
        }

        return server.GetWorld().GetDimension(DimensionType.Overworld);
    }
}










