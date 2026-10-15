using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(13)]
public sealed class AddActorPacket : DataPacket {
    public long ActorId;
    public ulong RuntimeId;
    public string ActorType = string.Empty;
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public Vec2 Rotation = new();
    public float HeadRotation;
    public float BodyRotation;
    public SyncedAttribute[] Attributes = [];
    public ActorDataItem[] ActorData = [];
    public PropertySyncData SynchedProperties = new();
    public ActorLink[] Links = [];
    public PassengerOfBlockArguments? PassengerOfBlock;

    public override void Deserialize(ref BinaryReader reader) {
        ActorId = reader.ReadZigZong();
        RuntimeId = reader.ReadVarULong();
        ActorType = reader.ReadVarString();
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        Rotation.Read(ref reader);
        HeadRotation = reader.ReadF32(true);
        BodyRotation = reader.ReadF32(true);
        Attributes = new SyncedAttribute[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Attributes.Length; i++) {
            Attributes[i] = new SyncedAttribute();
            Attributes[i].Read(ref reader);
        }
        ActorData = new ActorDataItem[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ActorData.Length; i++) {
            ActorData[i] = new ActorDataItem();
            ActorData[i].Read(ref reader);
        }
        SynchedProperties.Read(ref reader);
        Links = new ActorLink[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Links.Length; i++) {
            Links[i] = new ActorLink();
            Links[i].Read(ref reader);
        }
        PassengerOfBlock = reader.ReadBool() ? new PassengerOfBlockArguments() : null;
        PassengerOfBlock?.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZong(ActorId);
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarString(ActorType);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        writer.WriteF32(BodyRotation, true);
        writer.WriteVarUInt((uint)Attributes.Length);
        foreach (SyncedAttribute attribute in Attributes)
            attribute.Write(ref writer);
        writer.WriteVarUInt((uint)ActorData.Length);
        foreach (ActorDataItem item in ActorData)
            item.Write(ref writer);
        SynchedProperties.Write(ref writer);
        writer.WriteVarUInt((uint)Links.Length);
        foreach (ActorLink link in Links)
            link.Write(ref writer);
        writer.WriteBool(PassengerOfBlock is not null);
        PassengerOfBlock?.Write(ref writer);
    }
}
