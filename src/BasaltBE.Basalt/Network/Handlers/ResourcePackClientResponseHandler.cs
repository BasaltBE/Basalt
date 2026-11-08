using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ResourcePackClientResponseHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, ResourcePackClientResponsePacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.ResourcePackResponse");
        if (player is null) throw new InvalidDataException("Resource pack response arrived before player login.");

        switch (packet.Response) {
            case ResourcePackClientResponseDownloading downloading:
                SendPackData(player, downloading);
                break;
            case ResourcePackClientResponseDownloadingFinished:
                SendStack(player, includePacks: true);
                break;
            case ResourcePackClientResponseStackFinished:
                Logger.Scoped("Network").Debug($"Resource packs applied by {peer.ConnectionId}.");
                CompoundTag structureData = new();
                structureData.Add("processors", new ListTag());
                structureData.Add("template_pools", new ListTag());
                structureData.Add("jigsaws", new ListTag());
                structureData.Add("structure_sets", new ListTag());

                player.SendPacket(new JigsawStructureDataPacket {
                    StructureData = structureData
                });
                player.SendPacket(new VoxelShapesPacket());
                if (network.Server.CustomDimensions.Count > 0) {
                    network.SendPreparedPacket(player, network.SerializedPackets.Get<DimensionDataPacket>());
                    network.SendPreparedPacket(player, network.SerializedPackets.Get<BiomeDefinitionListPacket>());
                }
                player.SendPacket(new StartGamePacket {
                    EntityId = player.UniqueId,
                    RuntimeId = player.RuntimeId,
                    GameType = player.GameType,
                    LevelCurrentTime = player.Dimension.World is { } world
                        ? (ulong)((world.GameTime % 24000 + 24000) % 24000)
                        : player.Dimension.TickValue % 24000,
                    Position = player.GetHeadPosition(),
                    Settings = new LevelSettings {
                        Rules = player.Dimension.World?.GameRules.CreatePacketData() ?? new GameRulesChangedPacketData(),
                        SpawnSettings = new SpawnSettings {
                            SpawnBiomeType = SpawnBiomeType.Default,
                            Dimension = player.Dimension.RuntimeId
                        },
                        GeneratorType = player.Dimension.GeneratorType,
                        GameType = player.GameType,
                        GameDifficulty = player.Dimension.Difficulty,
                        DefaultSpawnBlockPosition = new BlockPos {
                            X = (int)MathF.Floor(player.Dimension.SpawnPosition.X),
                            Y = (int)MathF.Floor(player.Dimension.SpawnPosition.Y),
                            Z = (int)MathF.Floor(player.Dimension.SpawnPosition.Z)
                        },
                        MultiplayerGameIntent = true,
                        CommandsEnabled = true,
                        LanBroadcastIntent = true,
                        XboxLiveBroadcastSetting = GamePublishSetting.Public,
                        PlatformBroadcastSetting = GamePublishSetting.Public,
                        TexturePacksRequired = network.ResourcePacks.Required,
                        PlayerPermissions = PlayerPermissionLevel.Member,
                        BaseGameVersion = network.Advertisement.Version
                    },
                    LevelName = network.Advertisement.Level,
                    MovementSettings = new SyncedPlayerMovementSettings {
                        RewindHistorySize = 40,
                        ServerAuthoritativeBlockBreaking = true
                    },
                    MultiplayerCorrelationId = Guid.NewGuid().ToString(),
                    EnableItemStackNetManager = true,
                    ServerVersion = network.Advertisement.Version,
                    ServerTelemetryData = new ServerTelemetryData {
                        OwnerId = network.Advertisement.Name
                    },
                    BlockNetworkIdsAreHashes = true,
                    BlockProperties = BlockPalette.Properties
                });
                network.SendPreparedPacket(player, network.SerializedPackets.Get<ItemRegistryPacket>());
                player.SendPacket(player.Attributes.CreateUpdate(player.RuntimeId, all: true)!);
                player.SendPacket(player.Flags.CreateUpdate(player.RuntimeId));
                Volatile.Write(ref player.Permissions.Dirty, false);
                player.SendPacket(player.Abilities.CreateUpdate(player.UniqueId, player.Permissions));
                network.SendPreparedPacket(player, network.SerializedPackets.Get<AvailableActorIdentifiersPacket>());
                player.SendPacket(new PlayStatusPacket {
                    Status = PlayStatus.PlayerSpawn
                });
                player.SendPacket(new SetCommandsEnabledPacket { Enabled = true });
                var commands = network.Server.Commands.GetCatalog(player.Permissions);
                network.SendSerializedPacket(player, commands.Packet, commands.Payload);
                network.SendPreparedPacket(player, network.SerializedPackets.Get<CreativeContentPacket>());
                network.SendPreparedPacket(player, network.SerializedPackets.Get<CraftingDataPacket>());
                break;
            case ResourcePackClientResponseCancel:
                if (network.ResourcePacks.Required) {
                    const string message = "This server requires its resource packs.";
                    player.SendPacket(new DisconnectPacket {
                        Reason = DisconnectFailReason.Unknown,
                        Message = message,
                        FilteredMessage = message
                    });
                    break;
                }

                player.RequestedResourcePacks.Clear();
                SendStack(player, includePacks: false);
                break;
            default:
                throw new InvalidDataException("Unknown resource pack response.");
        }
    }

    private void SendPackData(Player player, ResourcePackClientResponseDownloading downloading) {
        foreach (string resourceName in downloading.PackIds) {
            if (!network.ResourcePacks.TryGet(resourceName, out ResourcePack? pack) || pack is null)
                throw new InvalidDataException($"Client requested unknown resource pack '{resourceName}'.");

            player.RequestedResourcePacks.Add(pack.Id);
            player.SendPacket(new ResourcePackDataInfoPacket {
                ResourceName = pack.ResourceName,
                ChunkSize = ResourcePackManager.ChunkSize,
                NumberOfChunks = pack.GetChunkCount(ResourcePackManager.ChunkSize),
                FileSize = (ulong)pack.ArchiveLength,
                FileHash = pack.Hash,
                PackType = 6
            });
        }
    }

    private void SendStack(Player player, bool includePacks) {
        ResourcePack[] packs = includePacks ? network.ResourcePacks.Packs : [];
        PackInstanceId[] texturePacks = new PackInstanceId[packs.Length];
        for (int i = 0; i < packs.Length; i++) {
            ResourcePack pack = packs[i];
            texturePacks[i] = new PackInstanceId {
                PackId = pack.Id.ToString(),
                Version = pack.Version
            };
        }

        player.SendPacket(new ResourcePackStackPacket {
            TexturePackRequired = network.ResourcePacks.Required,
            TexturePacks = texturePacks,
            BaseGameVersion = BedrockProtocol.MinecraftVersion
        });
    }
}
