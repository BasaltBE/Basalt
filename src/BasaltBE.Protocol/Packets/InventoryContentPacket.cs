using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(49)]
public sealed class InventoryContentPacket : DataPacket {
    public uint ContainerId;
    public NetworkItemStackDescriptor[] Slots = [];
    public FullContainerName FullContainerName = new();
    public NetworkItemStackDescriptor StorageItem = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt(ContainerId);
        writer.WriteVarUInt((uint)Slots.Length);
        foreach (NetworkItemStackDescriptor item in Slots) item.Write(ref writer);
        FullContainerName.Write(ref writer);
        StorageItem.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = reader.ReadVarUInt();
        uint count = reader.ReadVarUInt();
        Slots = new NetworkItemStackDescriptor[(int)count];
        for (int i = 0; i < Slots.Length; i++) {
            Slots[i] = new NetworkItemStackDescriptor();
            Slots[i].Read(ref reader);
        }
        FullContainerName.Read(ref reader);
        StorageItem.Read(ref reader);
    }
}
