using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventorySlotPacket : DataPacket
{
    public uint WindowId { get; set; }
    public uint Slot { get; set; }
    public FullContainerName Container { get; set; } = new();
    public ItemInstance StorageItem { get; set; } = new();
    public ItemInstance NewItem { get; set; } = new();

    public override PacketId PacketId => PacketId.InventorySlot;

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadVarUInt();
        Slot = reader.ReadVarUInt();
        Container.Read(ref reader);
        StorageItem.Read(ref reader);
        NewItem.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(WindowId);
        writer.WriteVarUInt(Slot);
        Container.Write(ref writer);
        StorageItem.Write(ref writer);
        NewItem.Write(ref writer);
    }
}
