using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record StartGamePacket : DataPacket
{
    private static readonly TagOptions TagOptions = new(Name: true, Type: true, VarInt: true);

    public long EntityUniqueId;
    public ulong EntityRuntimeId;
    public int PlayerGameMode;
    public Vec3f PlayerPosition;
    public float Pitch;
    public float Yaw;
    public long WorldSeed;
    public SpawnBiomeType SpawnBiomeType;
    public string UserDefinedBiomeName = string.Empty;
    public int Dimension;
    public int Generator;
    public int WorldGameMode;
    public bool Hardcore;
    public int Difficulty;
    public BlockPos WorldSpawn;
    public bool AchievementsDisabled;
    public EditorWorldType EditorWorldType;
    public bool CreatedInEditor;
    public bool ExportedFromEditor;
    public int DayCycleLockTime;
    public int EducationEditionOffer;
    public bool EducationFeaturesEnabled;
    public string EducationProductId = string.Empty;
    public float RainLevel;
    public float LightningLevel;
    public bool ConfirmedPlatformLockedContent;
    public bool MultiPlayerGame;
    public bool LanBroadcastEnabled;
    public XblBroadcastMode XblBroadcastMode;
    public int PlatformBroadcastMode;
    public bool CommandsEnabled;
    public bool TexturePackRequired;
    public List<GameRule> GameRules = [];
    public List<ExperimentData> Experiments = [];
    public bool ExperimentsPreviouslyToggled;
    public bool BonusChestEnabled;
    public bool StartWithMapEnabled;
    public int PlayerPermissions;
    public int ServerChunkTickRadius;
    public bool HasLockedBehaviourPack;
    public bool HasLockedTexturePack;
    public bool FromLockedWorldTemplate;
    public bool MsaGamerTagsOnly;
    public bool FromWorldTemplate;
    public bool WorldTemplateSettingsLocked;
    public bool OnlySpawnV1Villagers;
    public bool PersonaDisabled;
    public bool CustomSkinsDisabled;
    public bool EmoteChatMuted;
    public string BaseGameVersion = string.Empty;
    public int LimitedWorldWidth;
    public int LimitedWorldDepth;
    public bool NewNether;
    public EducationSharedResourceUri EducationSharedResourceUri = new();
    public Optional<BoolType> ForceExperimentalGameplay = new();
    public ChatRestrictionLevel ChatRestrictionLevel;
    public bool DisablePlayerInteractions;
    public string LevelId = string.Empty;
    public string WorldName = string.Empty;
    public string TemplateContentIdentity = string.Empty;
    public bool Trial;
    public PlayerMovementSettings PlayerMovementSettings = new();
    public long Time;
    public int EnchantmentSeed;
    public List<BlockEntry> Blocks = [];
    public string MultiPlayerCorrelationId = string.Empty;
    public bool ServerAuthoritativeInventory;
    public string GameVersion = string.Empty;
    public CompoundTag PropertyData = new();
    public ulong ServerBlockStateChecksum;
    public Guid WorldTemplateId = Guid.Empty;
    public bool ClientSideGeneration;
    public bool UseBlockNetworkIdHashes;
    public bool ServerAuthoritativeSound;
    public OptionalValue<ServerJoinInformation> ServerJoinInformation = new();
    public string ServerId = string.Empty;
    public string ScenarioId = string.Empty;
    public string WorldId = string.Empty;
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
        ServerBlockStateChecksum = reader.ReadUInt64(true);
        WorldTemplateId = UUID.Read(reader);
        ClientSideGeneration = reader.ReadBool();
        UseBlockNetworkIdHashes = reader.ReadBool();
        ServerAuthoritativeSound = reader.ReadBool();
        ServerJoinInformation.Read(reader, static (BinaryReader r) =>
        {
            ServerJoinInformation value = new();
            value.Read(r);
            return value;
        });
        ServerId = reader.ReadVarString();
        ScenarioId = reader.ReadVarString();
        WorldId = reader.ReadVarString();
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
        ServerJoinInformation.Write(writer, static (BinaryWriter w, ServerJoinInformation value) => value.Write(w));
        writer.WriteVarString(ServerId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(WorldId);
        writer.WriteVarString(OwnerId);
    }
}

