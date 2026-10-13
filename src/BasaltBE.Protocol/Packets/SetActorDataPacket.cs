using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(39)]
public sealed class SetActorDataPacket : DataPacket {
    public ulong ActorRuntimeId;
    public ActorDataItem[] ActorData = [];
    public PropertySyncData SynchedProperties = new();
    public ulong Tick;

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        ActorData = new ActorDataItem[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < ActorData.Length; i++) {
            ActorDataItem item = new();
            item.Read(ref reader);
            ActorData[i] = item;
        }
        SynchedProperties.Read(ref reader);
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteVarUInt((uint)ActorData.Length);
        foreach (ActorDataItem item in ActorData)
            item.Write(ref writer);
        SynchedProperties.Write(ref writer);
        writer.WriteVarULong(Tick);
    }
}
