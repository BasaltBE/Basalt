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

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadVarInt();
        Slot = reader.ReadVarInt();
        Container.Read(ref reader);
        StorageItem.Read(ref reader);
        NewItem.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarInt(Slot);
        Container.Write(ref writer);
        StorageItem.Write(ref writer);
        NewItem.Write(ref writer);
    }
}
