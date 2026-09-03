using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(12)]
public sealed class AddPlayerPacket : DataPacket {
    public Uuid Uuid = new();
    public string PlayerName = string.Empty;
    public ulong ActorRuntimeId;
    public string PlatformChatId = string.Empty;
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public Vec2 Rotation = new();
    public float HeadRotation;
    public NetworkItemStackDescriptor CarriedItem = new();
    public int PlayerGameType;
    public ActorDataList EntityData = new();
    public PropertySyncData SynchedProperties = new();
    public SerializedAbilitiesData AbilitiesData = new();
    public ActorLink[] ActorLinks = [];
    public string DeviceId = string.Empty;
    public int BuildPlatform;

    public override void Serialize(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        writer.WriteVarString(PlayerName);
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteVarString(PlatformChatId);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        CarriedItem.Write(ref writer);
        writer.WriteZigZong(PlayerGameType);
        EntityData.Write(ref writer);
        SynchedProperties.Write(ref writer);
        AbilitiesData.Write(ref writer);
        WriteLinks(ref writer);
        writer.WriteVarString(DeviceId);
        writer.WriteInt32(BuildPlatform, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        PlayerName = reader.ReadVarString();
        ActorRuntimeId = reader.ReadVarULong();
        PlatformChatId = reader.ReadVarString();
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        Rotation.Read(ref reader);
        HeadRotation = reader.ReadF32(true);
        CarriedItem.Read(ref reader);
        PlayerGameType = checked((int)reader.ReadZigZong());
        EntityData.Read(ref reader);
        SynchedProperties.Read(ref reader);
        AbilitiesData.Read(ref reader);
        ActorLinks = ReadLinks(ref reader);
        DeviceId = reader.ReadVarString();
        BuildPlatform = reader.ReadInt32(true);
    }

    void WriteLinks(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)ActorLinks.Length);
        for (int i = 0; i < ActorLinks.Length; i++) ActorLinks[i].Write(ref writer);
    }

    static ActorLink[] ReadLinks(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        ActorLink[] links = new ActorLink[count];
        for (int i = 0; i < count; i++) {
            links[i] = new ActorLink();
            links[i].Read(ref reader);
        }
        return links;
    }
}
