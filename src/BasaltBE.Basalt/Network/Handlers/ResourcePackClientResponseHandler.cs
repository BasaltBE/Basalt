using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Crafting;
using BasaltBE.Basalt.Items;
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
        if (player is null)
            throw new InvalidDataException("Resource pack response arrived before player login.");

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

                network.SendPacket(player, new JigsawStructureDataPacket {
                    StructureData = structureData
                });
                network.SendPacket(player, new VoxelShapesPacket());
                network.SendPacket(player, new StartGamePacket {
                    EntityId = player.UniqueId,
                    RuntimeId = player.RuntimeId,
                    GameType = GameType.Survival,
                    Position = player.GetHeadPosition(),
                    Settings = new LevelSettings {
                        SpawnSettings = new SpawnSettings {
                            SpawnBiomeType = SpawnBiomeType.Default,
                            Dimension = 0
                        },
                        GeneratorType = GeneratorType.Overworld,
                        GameType = GameType.Survival,
                        GameDifficulty = Difficulty.Normal,
                        DefaultSpawnBlockPosition = new BlockPos { X = 0, Y = -57, Z = 0 },
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
                        RewindHistorySize = 0,
                        ServerAuthoritativeBlockBreaking = true
                    },
                    MultiplayerCorrelationId = Guid.NewGuid().ToString(),
                    EnableItemStackNetManager = true,
                    ServerVersion = network.Advertisement.Version,
                    ServerTelemetryData = new ServerTelemetryData {
                        OwnerId = network.Advertisement.Name
                    },
                    BlockNetworkIdsAreHashes = true
                });
                network.SendSerializedPacket(player, ItemPalette.RegistryPacket, ItemPalette.SerializedRegistry);
                network.SendPacket(player, player.Attributes.CreateUpdate(player.RuntimeId, all: true, tick: network.Server.TickValue)!);
                network.SendPacket(player, player.Flags.CreateUpdate(player.RuntimeId, network.Server.TickValue));
                Volatile.Write(ref player.Permissions.Dirty, false);
                network.SendPacket(player, player.Abilities.CreateUpdate(player.UniqueId, player.Permissions));
                // TODO: Cache AvailableActorIdentifiers packet
                CompoundTag actorIdentifiers = new();
                ListTag identifierList = new();
                foreach (EntityType type in EntityPalette.Types) {
                    CompoundTag identifier = new();
                    identifier.Add("bid", new StringTag { Value = string.Empty });
                    identifier.Add("id", new StringTag { Value = type.Identifier });
                    identifierList.Value.Add(identifier);
                }
                actorIdentifiers.Add("idlist", identifierList);
                network.SendPacket(player, new AvailableActorIdentifiersPacket {
                    IdentifierList = actorIdentifiers
                });
                network.SendPacket(player, new PlayStatusPacket {
                    Status = PlayStatus.PlayerSpawn
                });
                network.SendPacket(player, new SetCommandsEnabledPacket { Enabled = true });
                var commands = network.Server.Commands.GetCatalog(player.Permissions);
                network.SendSerializedPacket(player, commands.Packet, commands.Payload);
                network.SendSerializedPacket(player, ItemPalette.CreativeContentPacket, ItemPalette.SerializedCreativeContent);
                network.SendSerializedPacket(player, CraftingPalette.Packet, CraftingPalette.SerializedPacket);
                break;
            case ResourcePackClientResponseCancel:
                if (network.ResourcePacks.Required) {
                    const string message = "This server requires its resource packs.";
                    network.SendPacket(player, new DisconnectPacket {
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
            network.SendPacket(player, new ResourcePackDataInfoPacket {
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

        network.SendPacket(player, new ResourcePackStackPacket {
            TexturePackRequired = network.ResourcePacks.Required,
            TexturePacks = texturePacks,
            BaseGameVersion = BedrockProtocol.MinecraftVersion
        });
    }
}
