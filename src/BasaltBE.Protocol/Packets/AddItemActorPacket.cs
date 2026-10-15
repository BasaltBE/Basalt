using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(15)]
public sealed class AddItemActorPacket : DataPacket {
    public long ActorId;
    public ulong RuntimeId;
    public NetworkItemStackDescriptor Item = new();
    public Vec3 Position = new();
    public Vec3 Velocity = new();
    public ActorDataItem[] ActorData = [];
    public bool IsFromFishing;

    public override void Deserialize(ref BinaryReader reader) {
        ActorId = reader.ReadZigZong();
        RuntimeId = reader.ReadVarULong();
        Item.Read(ref reader);
        Position.Read(ref reader);
        Velocity.Read(ref reader);
        ActorData = new ActorDataItem[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ActorData.Length; i++) {
            ActorData[i] = new ActorDataItem();
            ActorData[i].Read(ref reader);
        }
        IsFromFishing = reader.ReadBool();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZong(ActorId);
        writer.WriteVarULong(RuntimeId);
        Item.Write(ref writer);
        Position.Write(ref writer);
        Velocity.Write(ref writer);
        writer.WriteVarUInt((uint)ActorData.Length);
        foreach (ActorDataItem item in ActorData)
            item.Write(ref writer);
        writer.WriteBool(IsFromFishing);
    }
}
