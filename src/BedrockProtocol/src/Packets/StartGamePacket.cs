using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(11)]
public sealed class StartGamePacket : DataPacket {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public long EntityId;
    public ulong RuntimeId;
    public GameType GameType;
    public Vec3 Position = new();
    public Vec2 Rotation = new();
    public LevelSettings Settings = new();
    public string LevelId = string.Empty;
    public string LevelName = string.Empty;
    public string TemplateContentIdentity = string.Empty;
    public bool Trial;
    public SyncedPlayerMovementSettings MovementSettings = new();
    public ulong LevelCurrentTime;
    public int EnchantmentSeed;
    public ServerBlockProperty[] BlockProperties = [];
    public string MultiplayerCorrelationId = string.Empty;
    public bool EnableItemStackNetManager;
    public string ServerVersion = string.Empty;
    public CompoundTag PlayerPropertyData = new();
    public ulong ServerBlockTypeRegistryChecksum;
    public Uuid WorldTemplateId = new();
    public bool ServerEnabledClientSideGeneration;
    public bool BlockNetworkIdsAreHashes;
    public NetworkPermissions NetworkPermissions = new();
    public ServerConfigurationJoinInfo? ServerConfigurationJoinInfo;
    public ServerTelemetryData ServerTelemetryData = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZong(EntityId);
        writer.WriteVarULong(RuntimeId);
        writer.WriteZigZag((int)GameType);
        Position.Write(ref writer);
        Rotation.Write(ref writer);
        Settings.Write(ref writer);
        writer.WriteVarString(LevelId);
        writer.WriteVarString(LevelName);
        writer.WriteVarString(TemplateContentIdentity);
        writer.WriteBool(Trial);
        MovementSettings.Write(ref writer);
        writer.WriteUInt64(LevelCurrentTime, true);
        writer.WriteZigZag(EnchantmentSeed);
        writer.WriteVarUInt((uint)BlockProperties.Length);
        foreach (ServerBlockProperty property in BlockProperties) property.Write(ref writer);
        writer.WriteVarString(MultiplayerCorrelationId);
        writer.WriteBool(EnableItemStackNetManager);
        writer.WriteVarString(ServerVersion);
        Nbt.WriteTag(writer, PlayerPropertyData, NetworkNbtOptions);
        writer.WriteUInt64(ServerBlockTypeRegistryChecksum, true);
        WorldTemplateId.Write(ref writer);
        writer.WriteBool(ServerEnabledClientSideGeneration);
        writer.WriteBool(BlockNetworkIdsAreHashes);
        NetworkPermissions.Write(ref writer);
        writer.WriteBool(ServerConfigurationJoinInfo is not null);
        if (ServerConfigurationJoinInfo is not null) ServerConfigurationJoinInfo.Write(ref writer);
        ServerTelemetryData.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        EntityId = reader.ReadZigZong();
        RuntimeId = reader.ReadVarULong();
        GameType = (GameType)reader.ReadZigZag();
        Position.Read(ref reader);
        Rotation.Read(ref reader);
        Settings.Read(ref reader);
        LevelId = reader.ReadVarString();
        LevelName = reader.ReadVarString();
        TemplateContentIdentity = reader.ReadVarString();
        Trial = reader.ReadBool();
        MovementSettings.Read(ref reader);
        LevelCurrentTime = reader.ReadUInt64(true);
        EnchantmentSeed = reader.ReadZigZag();
        int blockCount = checked((int)reader.ReadVarUInt());
        BlockProperties = new ServerBlockProperty[blockCount];
        for (int index = 0; index < blockCount; index++) {
            ServerBlockProperty property = new();
            property.Read(ref reader);
            BlockProperties[index] = property;
        }
        MultiplayerCorrelationId = reader.ReadVarString();
        EnableItemStackNetManager = reader.ReadBool();
        ServerVersion = reader.ReadVarString();
        PlayerPropertyData = Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
        ServerBlockTypeRegistryChecksum = reader.ReadUInt64(true);
        WorldTemplateId.Read(ref reader);
        ServerEnabledClientSideGeneration = reader.ReadBool();
        BlockNetworkIdsAreHashes = reader.ReadBool();
        NetworkPermissions.Read(ref reader);
        ServerConfigurationJoinInfo = reader.ReadBool() ? new ServerConfigurationJoinInfo() : null;
        if (ServerConfigurationJoinInfo is not null) ServerConfigurationJoinInfo.Read(ref reader);
        ServerTelemetryData.Read(ref reader);
    }
}
