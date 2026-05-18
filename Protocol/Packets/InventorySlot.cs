using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventorySlotPacket : DataPacket
{
    public int WindowId { get; set; }
    public int Slot { get; set; }
    public Optional<FullContainerName> Container { get; set; } = new();
    public Optional<ItemInstanceNew> StorageItem { get; set; } = new();
    public ItemInstanceNew NewItem { get; set; } = new();

    public override PacketId PacketId => PacketId.InventorySlot;

    public override void Deserialize(BinaryReader reader)
    {
        WindowId = reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Container.Read(reader);
        StorageItem.Read(reader);
        NewItem.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarInt(Slot);
        Container.Write(writer);
        StorageItem.Write(writer);
        NewItem.Write(writer);
    }
}
