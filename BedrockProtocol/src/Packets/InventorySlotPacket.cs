using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(50)]
public sealed class InventorySlotPacket : DataPacket {
    public ContainerId ContainerId;
    public uint Slot;
    public FullContainerName? Container;
    public NetworkItemStackDescriptor? StorageItem;
    public NetworkItemStackDescriptor Item = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerId);
        writer.WriteVarUInt(Slot);
        writer.WriteBool(Container is not null);
        if (Container is FullContainerName container) container.Write(ref writer);
        writer.WriteBool(StorageItem is not null);
        if (StorageItem is NetworkItemStackDescriptor storageItem) storageItem.Write(ref writer);
        Item.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadUInt8();
        Slot = reader.ReadVarUInt();
        Container = reader.ReadBool() ? new FullContainerName() : null;
        Container?.Read(ref reader);
        StorageItem = reader.ReadBool() ? new NetworkItemStackDescriptor() : null;
        StorageItem?.Read(ref reader);
        Item.Read(ref reader);
    }
}
