using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record CreativeContentPacket : DataPacket
{
    public List<CreativeGroup> Groups { get; set; } = [];
    public List<CreativeItem> Items { get; set; } = [];

    public override PacketId PacketId => PacketId.CreativeContent;

    public override void Deserialize(BinaryReader reader)
    {
        int groupCount = checked((int)reader.ReadVarUInt());
        Groups = new List<CreativeGroup>(groupCount);
        for (int i = 0; i < groupCount; i++)
        {
            CreativeGroup group = new();
            group.Read(reader);
            Groups.Add(group);
        }

        int itemCount = checked((int)reader.ReadVarUInt());
        Items = new List<CreativeItem>(itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            CreativeItem item = new();
            item.Read(reader);
            Items.Add(item);
        }
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Groups.Count);
        for (int i = 0; i < Groups.Count; i++)
        {
            Groups[i].Write(writer);
        }

        writer.WriteVarUInt((uint)Items.Count);
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Write(writer);
        }
    }
}
