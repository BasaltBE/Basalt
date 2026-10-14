using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(162)]
public sealed class ItemRegistryPacket : DataPacket {
    public ItemData[] Items = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Items.Length);
        foreach (ItemData item in Items)
            item.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint itemCount = reader.ReadVarUInt();
        Items = new ItemData[checked((int)itemCount)];
        for (int i = 0; i < Items.Length; i++) {
            ItemData item = new();
            item.Read(ref reader);
            Items[i] = item;
        }
    }
}
