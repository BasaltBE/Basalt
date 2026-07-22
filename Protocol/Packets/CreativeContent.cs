using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.CreativeContent)]
public sealed record CreativeContentPacket : DataPacket {
    /// <summary>
    /// Creative item groups.
    /// </summary>
    public List<CreativeGroup> Groups = [];

    /// <summary>
    /// Creative item entries.
    /// </summary>
    public List<CreativeItem> Items = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        int groupCount = checked((int)reader.ReadVarUInt());
        Groups = new List<CreativeGroup>(groupCount);
        for (int i = 0; i < groupCount; i++) {
            CreativeGroup group = new();
            group.Read(reader);
            Groups.Add(group);
        }

        int itemCount = checked((int)reader.ReadVarUInt());
        Items = new List<CreativeItem>(itemCount);
        for (int i = 0; i < itemCount; i++) {
            CreativeItem item = new();
            item.Read(reader);
            Items.Add(item);
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarUInt((uint)Groups.Count);
        for (int i = 0; i < Groups.Count; i++) {
            Groups[i].Write(writer);
        }

        writer.WriteVarUInt((uint)Items.Count);
        for (int i = 0; i < Items.Count; i++) {
            Items[i].Write(writer);
        }
    }
}
