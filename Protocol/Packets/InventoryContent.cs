using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record InventoryContentPacket : DataPacket
{
    public int WindowId { get; set; }
    public List<NetworkItemStackDescriptor> Content { get; set; } = [];
    public FullContainerName Container { get; set; } = new();
    public NetworkItemStackDescriptor StorageItem { get; set; } = new();

    public override PacketId PacketId => PacketId.InventoryContent;

    public override void Deserialize(BinaryReader reader)
    {
        WindowId = reader.ReadVarInt();

        int count = checked((int)reader.ReadVarUInt());
        Content = new List<NetworkItemStackDescriptor>(count);
        for (int i = 0; i < count; i++)
        {
            NetworkItemStackDescriptor item = new();
            item.Read(reader);
            Content.Add(item);
        }

        Container.Read(reader);
        StorageItem.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarUInt((uint)Content.Count);
        for (int i = 0; i < Content.Count; i++)
        {
            Content[i].Write(writer);
        }

        Container.Write(writer);
        StorageItem.Write(writer);
    }
}
