using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.InventorySlot)]
public sealed record InventorySlotPacket : DataPacket
{
    /// <summary>
    /// Window id of the inventory.
    /// </summary>
    public int WindowId;

    /// <summary>
    /// Slot index in the container.
    /// </summary>
    public int Slot;

    /// <summary>
    /// Optional full container identity.
    /// </summary>
    public Optional<FullContainerName> Container = new();

    /// <summary>
    /// Optional storage item descriptor.
    /// </summary>
    public Optional<NetworkItemStackDescriptor> StorageItem = new();

    /// <summary>
    /// New item descriptor for this slot.
    /// </summary>
    public NetworkItemStackDescriptor NewItem = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        WindowId = reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Container.Read(reader);
        StorageItem.Read(reader);
        NewItem.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarInt(Slot);
        Container.Write(writer);
        StorageItem.Write(writer);
        NewItem.Write(writer);
    }
}
