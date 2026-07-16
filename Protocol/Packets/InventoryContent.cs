using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.InventoryContent)]
public sealed record InventoryContentPacket : DataPacket
{
    /// <summary>
    /// Container id for this inventory update.
    /// </summary>
    public ContainerId ContainerId;

    /// <summary>
    /// Inventory content entries.
    /// </summary>
    public List<NetworkItemStackDescriptor> Content = [];

    /// <summary>
    /// Full container identity.
    /// </summary>
    public FullContainerName Container = new();

    /// <summary>
    /// Optional storage item descriptor.
    /// </summary>
    public NetworkItemStackDescriptor StorageItem = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ContainerId = (ContainerId)reader.ReadVarInt();

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

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarInt((int)ContainerId);
        writer.WriteVarUInt((uint)Content.Count);
        for (int i = 0; i < Content.Count; i++)
        {
            Content[i].Write(writer);
        }

        Container.Write(writer);
        StorageItem.Write(writer);
    }
}
