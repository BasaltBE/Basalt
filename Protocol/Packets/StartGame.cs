using Basalt.Protocol.Enums;
using Basalt.Protocol.IO;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record StartGamePacket : DataPacket
{
    private static readonly ReadWriteOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public long EntityUniqueId { get; set; }
    public ulong EntityRuntimeId { get; set; }
    public int PlayerGameMode { get; set; }
    public Vec3f PlayerPosition { get; set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public long WorldSeed { get; set; }
    public SpawnBiomeType SpawnBiomeType { get; set; }
    public string UserDefinedBiomeName { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public int Generator { get; set; }
    public int WorldGameMode { get; set; }
    public bool Hardcore { get; set; }
    public int Difficulty { get; set; }
    public BlockPos WorldSpawn { get; set; }
    public bool AchievementsDisabled { get; set; }
    public EditorWorldType EditorWorldType { get; set; }
    public bool CreatedInEditor { get; set; }
    public bool ExportedFromEditor { get; set; }
    public int DayCycleLockTime { get; set; }
    public int EducationEditionOffer { get; set; }
    public bool EducationFeaturesEnabled { get; set; }
    public string EducationProductId { get; set; } = string.Empty;
    public float RainLevel { get; set; }
    public float LightningLevel { get; set; }
    public bool ConfirmedPlatformLockedContent { get; set; }
    public bool MultiPlayerGame { get; set; }
    public bool LanBroadcastEnabled { get; set; }
    public XblBroadcastMode XblBroadcastMode { get; set; }
    public int PlatformBroadcastMode { get; set; }
    public bool CommandsEnabled { get; set; }
    public bool TexturePackRequired { get; set; }
    public List<GameRule> GameRules { get; set; } = [];
    public List<ExperimentData> Experiments { get; set; } = [];
    public bool ExperimentsPreviouslyToggled { get; set; }
    public bool BonusChestEnabled { get; set; }
    public bool StartWithMapEnabled { get; set; }
    public int PlayerPermissions { get; set; }
    public int ServerChunkTickRadius { get; set; }
    public bool HasLockedBehaviourPack { get; set; }
    public bool HasLockedTexturePack { get; set; }
    public bool FromLockedWorldTemplate { get; set; }
    public bool MsaGamerTagsOnly { get; set; }
    public bool FromWorldTemplate { get; set; }
    public bool WorldTemplateSettingsLocked { get; set; }
    public bool OnlySpawnV1Villagers { get; set; }
    public bool PersonaDisabled { get; set; }
    public bool CustomSkinsDisabled { get; set; }
    public bool EmoteChatMuted { get; set; }
    public string BaseGameVersion { get; set; } = string.Empty;
    public int LimitedWorldWidth { get; set; }
    public int LimitedWorldDepth { get; set; }
    public bool NewNether { get; set; }
    public EducationSharedResourceUri EducationSharedResourceUri { get; set; } = new();
    public Optional<BoolType> ForceExperimentalGameplay { get; set; } = new();
    public ChatRestrictionLevel ChatRestrictionLevel { get; set; }
    public bool DisablePlayerInteractions { get; set; }
    public string LevelId { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;
    public string TemplateContentIdentity { get; set; } = string.Empty;
    public bool Trial { get; set; }
    public PlayerMovementSettings PlayerMovementSettings { get; set; } = new();
    public long Time { get; set; }
    public int EnchantmentSeed { get; set; }
    public List<BlockEntry> Blocks { get; set; } = [];
    public string MultiPlayerCorrelationId { get; set; } = string.Empty;
    public bool ServerAuthoritativeInventory { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public CompoundTag PropertyData { get; set; } = new();
    public ulong ServerBlockStateChecksum { get; set; }
    public Guid WorldTemplateId { get; set; } = Guid.Empty;
    public bool ClientSideGeneration { get; set; }
    public bool UseBlockNetworkIdHashes { get; set; }
    public bool ServerAuthoritativeSound { get; set; }
    public OptionalValue<ServerJoinInformation> ServerJoinInformation { get; set; } = new();
    public string ServerId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;

    public override PacketId PacketId => PacketId.StartGame;

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
            gameRule.ReadLegacy(reader);
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
        PropertyData = CompoundTag.Read(reader, NetworkNbtOptions, canHaveName: true);
        ServerBlockStateChecksum = reader.ReadUInt64(true);
        WorldTemplateId = UUID.Read(reader);
        ClientSideGeneration = reader.ReadBool();
        UseBlockNetworkIdHashes = reader.ReadBool();
        ServerAuthoritativeSound = reader.ReadBool();
        ServerJoinInformation.Read(reader, static (BinaryReader r) =>
        {
            ServerJoinInformation value = new();
            value.Read(ref r);
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
            GameRules[i].WriteLegacy(writer);
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
        NBT.WriteTag(writer, PropertyData, NetworkNbtOptions, canHaveName: true);
        writer.WriteUInt64(ServerBlockStateChecksum, true);
        UUID.Write(writer, WorldTemplateId);
        writer.WriteBool(ClientSideGeneration);
        writer.WriteBool(UseBlockNetworkIdHashes);
        writer.WriteBool(ServerAuthoritativeSound);
        ServerJoinInformation.Write(writer, static (BinaryWriter w, ServerJoinInformation value) => value.Write(ref w));
        writer.WriteVarString(ServerId);
        writer.WriteVarString(ScenarioId);
        writer.WriteVarString(WorldId);
        writer.WriteVarString(OwnerId);
    }
}

