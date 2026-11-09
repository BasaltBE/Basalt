using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(50)]
public sealed class InventorySlotPacket : DataPacket {
    public byte ContainerId;
    public uint Slot;
    public FullContainerName? FullContainerName;
    public NetworkItemStackDescriptor? StorageItem;
    public NetworkItemStackDescriptor Item = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8(ContainerId);
        writer.WriteVarUInt(Slot);
        writer.WriteBool(FullContainerName is not null);
        FullContainerName?.Write(ref writer);
        writer.WriteBool(StorageItem is not null);
        StorageItem?.Write(ref writer);
        Item.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = reader.ReadUInt8();
        Slot = reader.ReadVarUInt();
        FullContainerName = reader.ReadBool() ? new FullContainerName() : null;
        FullContainerName?.Read(ref reader);
        StorageItem = reader.ReadBool() ? new NetworkItemStackDescriptor() : null;
        StorageItem?.Read(ref reader);
        Item.Read(ref reader);
    }
}
