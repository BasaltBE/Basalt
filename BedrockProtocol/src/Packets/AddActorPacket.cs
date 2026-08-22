using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(13)]
public sealed class AddActorPacket : DataPacket {
    public long ActorUniqueId;
    public ulong ActorRuntimeId;
    public string ActorType = string.Empty;
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public Vec2 Rotation = new();
    public float HeadRotation;
    public float BodyRotation;
    public SyncedAttribute[] Attributes = [];
    public ActorDataList ActorData = new();
    public PropertySyncData SynchedProperties = new();
    public ActorLink[] ActorLinks = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarLong(ActorUniqueId);
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteVarString(ActorType);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        Rotation.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        writer.WriteF32(BodyRotation, true);
        WriteArray(ref writer, Attributes);
        ActorData.Write(ref writer);
        SynchedProperties.Write(ref writer);
        WriteArray(ref writer, ActorLinks);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorUniqueId = reader.ReadVarLong();
        ActorRuntimeId = reader.ReadVarULong();
        ActorType = reader.ReadVarString();
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        Rotation.Read(ref reader);
        HeadRotation = reader.ReadF32(true);
        BodyRotation = reader.ReadF32(true);
        Attributes = ReadArray<SyncedAttribute>(ref reader);
        ActorData.Read(ref reader);
        SynchedProperties.Read(ref reader);
        ActorLinks = ReadArray<ActorLink>(ref reader);
    }

    static void WriteArray<T>(ref BinaryWriter writer, T[] values) where T : DataType {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    static T[] ReadArray<T>(ref BinaryReader reader) where T : DataType, new() {
        int count = checked((int)reader.ReadVarUInt());
        T[] values = new T[count];
        for (int i = 0; i < count; i++) {
            values[i] = new T();
            values[i].Read(ref reader);
        }
        return values;
    }
}
