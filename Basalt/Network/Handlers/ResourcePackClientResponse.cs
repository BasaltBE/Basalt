using Basalt.Core;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ResourcePackClientResponse
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ResourcePackClientResponsePacket packet = new();
        packet.Deserialize(packetBuffer);

        switch (packet.Response)
        {
            case ResourcePackResponse.Refused:
                DisconnectPacket disconnect = new()
                {
                    Reason = DisconnectReason.ResourcePackProblem,
                    HideDisconnectionScreen = false,
                    Message = "Required resource packs were refused.",
                    FilteredMessage = "Required resource packs were refused."
                };
                server.Network.SendPacket(connection, disconnect);
                return;

            case ResourcePackResponse.SendPacks:
                Console.WriteLine($"Client requested packs ({packet.PacksToDownload.Count}). Pack transfer is not implemented yet.");
                return;

            case ResourcePackResponse.AllPacksDownloaded:
                ResourcePackStackPacket stack = new()
                {
                    MustAccept = false,
                    Packs =
                    [
                        new ResourcePackStackEntry
                        {
                            Uuid = Guid.Parse("0fba4063-dba1-4281-9b89-ff9390653530"),
                            Version = "1.0.0",
                            SubPackName = string.Empty
                        }
                    ],
                    BaseGameVersion = ProtocolInfo.MinecraftVersion,
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    IncludeEditorPacks = true
                };
                server.Network.SendPacket(connection, stack);
                return;

            case ResourcePackResponse.Completed:
                if (!server.Players.TryGetValue(connection, out Player? player))
                {
                    Console.WriteLine("Resource pack flow completed, but no player session was found.");
                    return;
                }

                StartGamePacket startGame = new()
                {
                    EntityUniqueId = 1,
                    EntityRuntimeId = 1,
                    PlayerGameMode = 1,
                    PlayerPosition = new Vec3f { X = 0f, Y = -57f, Z = 0f },
                    Pitch = 0f,
                    Yaw = 0f,
                    WorldSeed = 0,
                    SpawnBiomeType = SpawnBiomeType.Default,
                    UserDefinedBiomeName = "plains",
                    Dimension = 0,
                    Generator = 1,
                    WorldGameMode = 1,
                    Hardcore = false,
                    Difficulty = 1,
                    WorldSpawn = new BlockPos { X = 0, Y = -58, Z = 0 },
                    AchievementsDisabled = false,
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
                    CommandsEnabled = true,
                    TexturePackRequired = false,
                    GameRules = [],
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    BonusChestEnabled = false,
                    StartWithMapEnabled = false,
                    PlayerPermissions = 1,
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
                    BaseGameVersion = ProtocolInfo.MinecraftVersion,
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
                    Blocks = [],
                    MultiPlayerCorrelationId = Guid.NewGuid().ToString(),
                    ServerAuthoritativeInventory = true,
                    GameVersion = ProtocolInfo.MinecraftVersion,
                    PropertyData = new Basalt.Protocol.Nbt.CompoundTag(),
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

                PlayStatusPacket spawnStatus = new()
                {
                    Status = PlayStatus.PlayerSpawn
                };

                server.Network.SendPackets(connection, [startGame, spawnStatus]);
                Console.WriteLine($"Resource pack flow completed. Sent StartGame + PlayerSpawn to {player.Username}.");
                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {(byte)packet.Response}");
                return;
        }
    }
}
