using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventoryContentPacket : DataPacket
{
    public uint WindowId { get; set; }
    public List<ItemInstance> Content { get; set; } = [];
    public FullContainerName Container { get; set; } = new();
    public ItemInstance StorageItem { get; set; } = new();

    public override PacketId PacketId => PacketId.InventoryContent;

    public override void Deserialize(ref BinaryReader reader)
    {
        WindowId = reader.ReadVarUInt();

        int count = checked((int)reader.ReadVarUInt());
        Content = new List<ItemInstance>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = new();
            item.Read(ref reader);
            Content.Add(item);
        }

        Container.Read(ref reader);
        StorageItem.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(WindowId);
        writer.WriteVarUInt((uint)Content.Count);
        for (int i = 0; i < Content.Count; i++)
        {
            Content[i].Write(ref writer);
        }

        Container.Write(ref writer);
        StorageItem.Write(ref writer);
    }
}
