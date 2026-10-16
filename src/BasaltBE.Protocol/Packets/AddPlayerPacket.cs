using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(12)]
public sealed class AddPlayerPacket : DataPacket
{
    public Uuid Uuid = new();
    public string Name = string.Empty;
    public ulong RuntimeId;
    public string PlatformChatId = string.Empty;
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public Vec2 Rotation = new();
    public float HeadRotation;
    public NetworkItemStackDescriptor CarriedItem = new();
    public GameType GameType;
    public ActorDataItem[] ActorData = [];
    public PropertySyncData SynchedProperties = new();
    public SerializedAbilitiesData Abilities = new();
    public ActorLink[] Links = [];
    public PassengerOfBlockArguments? PassengerOfBlock;
    public string DeviceId = string.Empty;
    public BuildPlatform BuildPlatform;

    public override void Deserialize(ref BinaryReader reader)
    {
        Uuid.Read(ref reader);
        Name = reader.ReadVarString();
        RuntimeId = reader.ReadVarULong();
        PlatformChatId = reader.ReadVarString();
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        Rotation.Read(ref reader);
        HeadRotation = reader.ReadF32(true);
        CarriedItem.Read(ref reader);
        GameType = (GameType)reader.ReadZigZag();
        ActorData = new ActorDataItem[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ActorData.Length; i++)
        {
            ActorData[i] = new ActorDataItem();
            ActorData[i].Read(ref reader);
        }
        SynchedProperties.Read(ref reader);
        Abilities.Read(ref reader);
        Links = new ActorLink[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Links.Length; i++)
        {
            Links[i] = new ActorLink();
            Links[i].Read(ref reader);
        }
        PassengerOfBlock = reader.ReadBool() ? new PassengerOfBlockArguments() : null;
        PassengerOfBlock?.Read(ref reader);
        DeviceId = reader.ReadVarString();
        BuildPlatform = (BuildPlatform)reader.ReadInt32(true);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        Uuid.Write(ref writer);
        writer.WriteVarString(Name);
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarString(PlatformChatId);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        CarriedItem.Write(ref writer);
        writer.WriteZigZag((int)GameType);
        writer.WriteVarUInt((uint)ActorData.Length);
        foreach (ActorDataItem item in ActorData)
            item.Write(ref writer);
        SynchedProperties.Write(ref writer);
        Abilities.Write(ref writer);
        writer.WriteVarUInt((uint)Links.Length);
        foreach (ActorLink link in Links)
            link.Write(ref writer);
        writer.WriteBool(PassengerOfBlock is not null);
        PassengerOfBlock?.Write(ref writer);
        writer.WriteVarString(DeviceId);
        writer.WriteInt32((int)BuildPlatform, true);
    }
}
