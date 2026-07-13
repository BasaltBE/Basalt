using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

/// <summary>
/// @Direction Clientbound
/// StartGamePacket is sent by the server to client when the client finishes its login sequence and is done loading resource packs
/// </summary>
[Packet(PacketId.StartGame)]
public sealed record StartGamePacket : DataPacket
{
    private static readonly TagOptions TagOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// Unique actor id of the player.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Runtime actor id of the player.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Initial player game mode.
    /// </summary>
    public int PlayerGameMode;

    /// <summary>
    /// Initial player position.
    /// </summary>
    public Vec3f PlayerPosition;

    /// <summary>
    /// Initial camera pitch.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Initial camera yaw.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// World seed value.
    /// </summary>
    public long WorldSeed;

    /// <summary>
    /// Spawn biome type selector.
    /// </summary>
    public SpawnBiomeType SpawnBiomeType;

    /// <summary>
    /// Custom biome name when user-defined.
    /// </summary>
    public string UserDefinedBiomeName = string.Empty;

    /// <summary>
    /// Dimension id.
    /// </summary>
    public int Dimension;

    /// <summary>
    /// World generator id.
    /// </summary>
    public int Generator;

    /// <summary>
    /// World default game mode.
    /// </summary>
    public int WorldGameMode;

    /// <summary>
    /// Whether hardcore mode is enabled.
    /// </summary>
    public bool Hardcore;

    /// <summary>
    /// World difficulty.
    /// </summary>
    public int Difficulty;

    /// <summary>
    /// World spawn position.
    /// </summary>
    public BlockPos WorldSpawn;

    /// <summary>
    /// Whether achievements are disabled.
    /// </summary>
    public bool AchievementsDisabled;

    /// <summary>
    /// Editor world type.
    /// </summary>
    public EditorWorldType EditorWorldType;

    /// <summary>
    /// Whether the world was created in editor.
    /// </summary>
    public bool CreatedInEditor;

    /// <summary>
    /// Whether the world was exported from editor.
    /// </summary>
    public bool ExportedFromEditor;

    /// <summary>
    /// Editor connection policy enforced by the server.
    /// </summary>
    public int ServerEditorConnectionPolicy;

    /// <summary>
    /// Whether anonymous block drops are allowed in editor worlds.
    /// </summary>
    public bool AllowAnonymousBlockDropsInEditorWorlds;

    /// <summary>
    /// Day cycle lock time.
    /// </summary>
    public int DayCycleLockTime;

    /// <summary>
    /// Education edition offer type.
    /// </summary>
    public int EducationEditionOffer;

    /// <summary>
    /// Whether education features are enabled.
    /// </summary>
    public bool EducationFeaturesEnabled;

    /// <summary>
    /// Education product id.
    /// </summary>
    public string EducationProductId = string.Empty;

    /// <summary>
    /// Current rain level.
    /// </summary>
    public float RainLevel;

    /// <summary>
    /// Current lightning level.
    /// </summary>
    public float LightningLevel;

    /// <summary>
    /// Whether platform-locked content is confirmed.
    /// </summary>
    public bool ConfirmedPlatformLockedContent;

    /// <summary>
    /// Whether multiplayer is enabled.
    /// </summary>
    public bool MultiPlayerGame;

    /// <summary>
    /// Whether LAN broadcast is enabled.
    /// </summary>
    public bool LanBroadcastEnabled;

    /// <summary>
    /// Xbox Live broadcast mode.
    /// </summary>
    public XblBroadcastMode XblBroadcastMode;

    /// <summary>
    /// Platform broadcast mode.
    /// </summary>
    public int PlatformBroadcastMode;

    /// <summary>
    /// Whether commands are enabled.
    /// </summary>
    public bool CommandsEnabled;

    /// <summary>
    /// Whether texture packs are required.
    /// </summary>
    public bool TexturePackRequired;

    /// <summary>
    /// World gamerules.
    /// </summary>
    public List<GameRule> GameRules = [];

    /// <summary>
    /// Experiment toggles.
    /// </summary>
    public List<ExperimentData> Experiments = [];

    /// <summary>
    /// Whether experiments were previously toggled.
    /// </summary>
    public bool ExperimentsPreviouslyToggled;

    /// <summary>
    /// Whether bonus chest is enabled.
    /// </summary>
    public bool BonusChestEnabled;

    /// <summary>
    /// Whether starting map is enabled.
    /// </summary>
    public bool StartWithMapEnabled;

    /// <summary>
    /// Player permission level.
    /// </summary>
    public int PlayerPermissions;

    /// <summary>
    /// Server chunk tick radius.
    /// </summary>
    public int ServerChunkTickRadius;

    /// <summary>
    /// Whether behavior pack is locked.
    /// </summary>
    public bool HasLockedBehaviourPack;

    /// <summary>
    /// Whether texture pack is locked.
    /// </summary>
    public bool HasLockedTexturePack;

    /// <summary>
    /// Whether world is from a locked template.
    /// </summary>
    public bool FromLockedWorldTemplate;

    /// <summary>
    /// Whether only MSA gamer tags are allowed.
    /// </summary>
    public bool MsaGamerTagsOnly;

    /// <summary>
    /// Whether world is from a template.
    /// </summary>
    public bool FromWorldTemplate;

    /// <summary>
    /// Whether world template settings are locked.
    /// </summary>
    public bool WorldTemplateSettingsLocked;

    /// <summary>
    /// Whether only v1 villagers can spawn.
    /// </summary>
    public bool OnlySpawnV1Villagers;

    /// <summary>
    /// Whether persona is disabled.
    /// </summary>
    public bool PersonaDisabled;

    /// <summary>
    /// Whether custom skins are disabled.
    /// </summary>
    public bool CustomSkinsDisabled;

    /// <summary>
    /// Whether emote chat is muted.
    /// </summary>
    public bool EmoteChatMuted;

    /// <summary>
    /// Base game version string.
    /// </summary>
    public string BaseGameVersion = string.Empty;

    /// <summary>
    /// Limited world width.
    /// </summary>
    public int LimitedWorldWidth;

    /// <summary>
    /// Limited world depth.
    /// </summary>
    public int LimitedWorldDepth;

    /// <summary>
    /// Whether new nether generation is enabled.
    /// </summary>
    public bool NewNether;

    /// <summary>
    /// Education shared resource URI.
    /// </summary>
    public EducationSharedResourceUri EducationSharedResourceUri = new();

    /// <summary>
    /// Optional force experimental gameplay flag.
    /// </summary>
    public Optional<BoolType> ForceExperimentalGameplay = new();

    /// <summary>
    /// Chat restriction level.
    /// </summary>
    public ChatRestrictionLevel ChatRestrictionLevel;

    /// <summary>
    /// Whether player interactions are disabled.
    /// </summary>
    public bool DisablePlayerInteractions;

    /// <summary>
    /// Level id.
    /// </summary>
    public string LevelId = string.Empty;

    /// <summary>
    /// World display name.
    /// </summary>
    public string WorldName = string.Empty;

    /// <summary>
    /// Template content identity.
    /// </summary>
    public string TemplateContentIdentity = string.Empty;

    /// <summary>
    /// Whether the world is a trial.
    /// </summary>
    public bool Trial;

    /// <summary>
    /// Player movement settings.
    /// </summary>
    public PlayerMovementSettings PlayerMovementSettings = new();

    /// <summary>
    /// World time.
    /// </summary>
    public long Time;

    /// <summary>
    /// Enchantment seed.
    /// </summary>
    public int EnchantmentSeed;

    /// <summary>
    /// Block palette entries.
    /// </summary>
    public List<BlockEntry> Blocks = [];

    /// <summary>
    /// Multiplayer correlation id.
    /// </summary>
    public string MultiPlayerCorrelationId = string.Empty;

    /// <summary>
    /// Whether inventory is server authoritative.
    /// </summary>
    public bool ServerAuthoritativeInventory;

    /// <summary>
    /// Server game version string.
    /// </summary>
    public string GameVersion = string.Empty;

    /// <summary>
    /// Level property data as NBT.
    /// </summary>
    public CompoundTag PropertyData = new();

    /// <summary>
    /// Block state checksum from server.
    /// </summary>
    public ulong ServerBlockStateChecksum;

    /// <summary>
    /// World template UUID.
    /// </summary>
    public Guid WorldTemplateId = Guid.Empty;

    /// <summary>
    /// Whether client-side generation is enabled.
    /// </summary>
    public bool ClientSideGeneration;

    /// <summary>
    /// Whether block network id hashes are used.
    /// </summary>
    public bool UseBlockNetworkIdHashes;

    /// <summary>
    /// Whether sound is server authoritative.
    /// </summary>
    public bool ServerAuthoritativeSound;

    /// <summary>
    /// Whether the server records chat messages.
    /// </summary>
    public bool LoggingChat;

    /// <summary>
    /// Optional server join information.
    /// </summary>
    public OptionalValue<ServerJoinInformation> ServerJoinInformation = new();

    /// <summary>
    /// Server id.
    /// </summary>
    public string ServerId = string.Empty;

    /// <summary>
    /// Scenario id.
    /// </summary>
    public string ScenarioId = string.Empty;

    /// <summary>
    /// World id.
    /// </summary>
    public string WorldId = string.Empty;

    /// <summary>
    /// Owner id.
    /// </summary>
    public string OwnerId = string.Empty;


    public override void Deserialize(BinaryReader reader)
    {
        EntityUniqueId = reader.ReadZigZong();
        EntityRuntimeId = reader.ReadVarULong();
        PlayerGameMode = reader.ReadZigZag();
        PlayerPosition.Read(reader);
        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        WorldSeed = reader.ReadInt64(true);
        SpawnBiomeType = (SpawnBiomeType)reader.ReadInt16(true);
        UserDefinedBiomeName = reader.ReadVarString();
        Dimension = reader.ReadZigZag();
        Generator = reader.ReadZigZag();
        WorldGameMode = reader.ReadZigZag();
        Hardcore = reader.ReadBool();
        Difficulty = reader.ReadZigZag();
        WorldSpawn.Read(reader);
        AchievementsDisabled = reader.ReadBool();
        EditorWorldType = (EditorWorldType)reader.ReadZigZag();
        CreatedInEditor = reader.ReadBool();
        ExportedFromEditor = reader.ReadBool();
        DayCycleLockTime = reader.ReadZigZag();
        EducationEditionOffer = reader.ReadZigZag();
        EducationFeaturesEnabled = reader.ReadBool();
        EducationProductId = reader.ReadVarString();
        RainLevel = reader.ReadF32(true);
        LightningLevel = reader.ReadF32(true);
        ConfirmedPlatformLockedContent = reader.ReadBool();
        MultiPlayerGame = reader.ReadBool();
        LanBroadcastEnabled = reader.ReadBool();
        XblBroadcastMode = (XblBroadcastMode)reader.ReadZigZag();
        PlatformBroadcastMode = reader.ReadZigZag();
        CommandsEnabled = reader.ReadBool();
        TexturePackRequired = reader.ReadBool();

        int gameRuleCount = checked((int)reader.ReadVarUInt());
        GameRules = new List<GameRule>(gameRuleCount);
        for (int i = 0; i < gameRuleCount; i++)
        {
            GameRule gameRule = new();
            gameRule.Read(reader);
            GameRules.Add(gameRule);
        }

        int experimentCount = checked((int)reader.ReadUInt32(true));
        Experiments = new List<ExperimentData>(experimentCount);
        for (int i = 0; i < experimentCount; i++)
        {
            ExperimentData experiment = new();
            experiment.Read(reader);
            Experiments.Add(experiment);
        }

        ExperimentsPreviouslyToggled = reader.ReadBool();
        BonusChestEnabled = reader.ReadBool();
        StartWithMapEnabled = reader.ReadBool();
        PlayerPermissions = reader.ReadZigZag();
        ServerChunkTickRadius = reader.ReadInt32(true);
        HasLockedBehaviourPack = reader.ReadBool();
        HasLockedTexturePack = reader.ReadBool();
        FromLockedWorldTemplate = reader.ReadBool();
        MsaGamerTagsOnly = reader.ReadBool();
        FromWorldTemplate = reader.ReadBool();
        WorldTemplateSettingsLocked = reader.ReadBool();
        OnlySpawnV1Villagers = reader.ReadBool();
        PersonaDisabled = reader.ReadBool();
        CustomSkinsDisabled = reader.ReadBool();
        EmoteChatMuted = reader.ReadBool();
        BaseGameVersion = reader.ReadVarString();
        LimitedWorldWidth = reader.ReadInt32(true);
        LimitedWorldDepth = reader.ReadInt32(true);
        NewNether = reader.ReadBool();
        EducationSharedResourceUri.Read(reader);
        ForceExperimentalGameplay.Read(reader);
        ChatRestrictionLevel = (ChatRestrictionLevel)reader.ReadUInt8();
        DisablePlayerInteractions = reader.ReadBool();
        ServerEditorConnectionPolicy = reader.ReadZigZag();
        AllowAnonymousBlockDropsInEditorWorlds = reader.ReadBool();
        LevelId = reader.ReadVarString();
        WorldName = reader.ReadVarString();
        TemplateContentIdentity = reader.ReadVarString();
        Trial = reader.ReadBool();
        PlayerMovementSettings.Read(reader);
        Time = reader.ReadInt64(true);
        EnchantmentSeed = reader.ReadZigZag();

        int blocksCount = checked((int)reader.ReadVarUInt());
        Blocks = new List<BlockEntry>(blocksCount);
        for (int i = 0; i < blocksCount; i++)
        {
            BlockEntry block = new();
            block.Read(reader);
            Blocks.Add(block);
        }

        MultiPlayerCorrelationId = reader.ReadVarString();
        ServerAuthoritativeInventory = reader.ReadBool();
        GameVersion = reader.ReadVarString();
        PropertyData = CompoundTag.Read(reader, TagOptions);
        if (reader.Remaining < sizeof(ulong))
        {
            return;
        }

        ServerBlockStateChecksum = reader.ReadUInt64(true);
        if (reader.Remaining < 16)
        {
            return;
        }

        WorldTemplateId = UUID.Read(reader);
        if (reader.Remaining < 1)
        {
            return;
        }

        ClientSideGeneration = reader.ReadBool();
        if (reader.Remaining < 1)
        {
            return;
        }

        UseBlockNetworkIdHashes = reader.ReadBool();
        if (reader.Remaining < 1)
        {
            return;
        }

        ServerAuthoritativeSound = reader.ReadBool();
        if (reader.Remaining < 1)
        {
            return;
        }

        LoggingChat = reader.ReadBool();
        if (reader.Remaining < 1)
        {
            return;
        }

        ServerJoinInformation.Read(reader, static (BinaryReader r) =>
        {
            ServerJoinInformation value = new();
            value.Read(r);
            return value;
        });
        if (reader.Remaining < 1)
        {
            return;
        }

        ServerId = reader.ReadVarString();
        if (reader.Remaining < 1)
        {
            return;
        }

        ScenarioId = reader.ReadVarString();
        if (reader.Remaining < 1)
        {
            return;
        }

        WorldId = reader.ReadVarString();
        if (reader.Remaining < 1)
        {
            return;
        }

        OwnerId = reader.ReadVarString();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteZigZong(EntityUniqueId);
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteZigZag(PlayerGameMode);
        PlayerPosition.Write(writer);
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        writer.WriteInt64(WorldSeed, true);
        writer.WriteInt16((short)SpawnBiomeType, true);
        writer.WriteVarString(UserDefinedBiomeName);
        writer.WriteZigZag(Dimension);
        writer.WriteZigZag(Generator);
        writer.WriteZigZag(WorldGameMode);
        writer.WriteBool(Hardcore);
        writer.WriteZigZag(Difficulty);
        WorldSpawn.Write(writer);
        writer.WriteBool(AchievementsDisabled);
        writer.WriteZigZag((int)EditorWorldType);
        writer.WriteBool(CreatedInEditor);
        writer.WriteBool(ExportedFromEditor);
        writer.WriteZigZag(DayCycleLockTime);
        writer.WriteZigZag(EducationEditionOffer);
        writer.WriteBool(EducationFeaturesEnabled);
        writer.WriteVarString(EducationProductId);
        writer.WriteF32(RainLevel, true);
        writer.WriteF32(LightningLevel, true);
        writer.WriteBool(ConfirmedPlatformLockedContent);
        writer.WriteBool(MultiPlayerGame);
        writer.WriteBool(LanBroadcastEnabled);
        writer.WriteZigZag((int)XblBroadcastMode);
        writer.WriteZigZag(PlatformBroadcastMode);
        writer.WriteBool(CommandsEnabled);
        writer.WriteBool(TexturePackRequired);

        writer.WriteVarUInt((uint)GameRules.Count);
        for (int i = 0; i < GameRules.Count; i++)
        {
            GameRules[i].Write(writer);
        }

        writer.WriteUInt32((uint)Experiments.Count, true);
        for (int i = 0; i < Experiments.Count; i++)
        {
            Experiments[i].Write(writer);
        }

        writer.WriteBool(ExperimentsPreviouslyToggled);
        writer.WriteBool(BonusChestEnabled);
        writer.WriteBool(StartWithMapEnabled);
        writer.WriteZigZag(PlayerPermissions);
        writer.WriteInt32(ServerChunkTickRadius, true);
        writer.WriteBool(HasLockedBehaviourPack);
        writer.WriteBool(HasLockedTexturePack);
        writer.WriteBool(FromLockedWorldTemplate);
        writer.WriteBool(MsaGamerTagsOnly);
        writer.WriteBool(FromWorldTemplate);
        writer.WriteBool(WorldTemplateSettingsLocked);
        writer.WriteBool(OnlySpawnV1Villagers);
        writer.WriteBool(PersonaDisabled);
        writer.WriteBool(CustomSkinsDisabled);
        writer.WriteBool(EmoteChatMuted);
        writer.WriteVarString(BaseGameVersion);
        writer.WriteInt32(LimitedWorldWidth, true);
        writer.WriteInt32(LimitedWorldDepth, true);
        writer.WriteBool(NewNether);
        EducationSharedResourceUri.Write(writer);
        ForceExperimentalGameplay.Write(writer);
        writer.WriteUInt8((byte)ChatRestrictionLevel);
        writer.WriteBool(DisablePlayerInteractions);
        writer.WriteZigZag(ServerEditorConnectionPolicy);
        writer.WriteBool(AllowAnonymousBlockDropsInEditorWorlds);
        writer.WriteVarString(LevelId);
        writer.WriteVarString(WorldName);
        writer.WriteVarString(TemplateContentIdentity);
        writer.WriteBool(Trial);
        PlayerMovementSettings.Write(writer);
        writer.WriteInt64(Time, true);
        writer.WriteZigZag(EnchantmentSeed);

        writer.WriteVarUInt((uint)Blocks.Count);
        for (int i = 0; i < Blocks.Count; i++)
        {
            Blocks[i].Write(writer);
        }

        writer.WriteVarString(MultiPlayerCorrelationId);
        writer.WriteBool(ServerAuthoritativeInventory);
        writer.WriteVarString(GameVersion);
        Io.NBT.WriteTag(writer, PropertyData, TagOptions);
        writer.WriteUInt64(ServerBlockStateChecksum, true);
        UUID.Write(writer, WorldTemplateId);
        writer.WriteBool(ClientSideGeneration);
        writer.WriteBool(UseBlockNetworkIdHashes);
        writer.WriteBool(ServerAuthoritativeSound);
        writer.WriteBool(LoggingChat);
        ServerJoinInformation.Write(writer, static (BinaryWriter w, ServerJoinInformation value) => value.Write(w));
        writer.WriteVarString(ServerId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(WorldId);
        writer.WriteVarString(OwnerId);
    }
}
