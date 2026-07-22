using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ItemRegistry)]
public sealed record ItemRegistryPacket : DataPacket {
    /// <summary>
    /// Item registry entries.
    /// </summary>
    public List<ItemEntry> Items = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Items = new List<ItemEntry>(count);
        for (int i = 0; i < count; i++) {
            ItemEntry entry = new();
            entry.Read(reader);
            Items.Add(entry);
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarUInt((uint)Items.Count);
        for (int i = 0; i < Items.Count; i++) {
            Items[i].Write(writer);
        }
    }
}
