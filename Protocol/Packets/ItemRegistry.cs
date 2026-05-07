using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ItemRegistryPacket : DataPacket
{
    public List<ItemEntry> Items { get; set; } = [];

    public override PacketId PacketId => PacketId.ItemRegistry;

    public override void Deserialize(ref BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        Items = new List<ItemEntry>(count);

        for (int i = 0; i < count; i++)
        {
            ItemEntry entry = new();
            entry.Read(ref reader);
            Items.Add(entry);
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Items.Count);
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Write(ref writer);
        }
    }
}
