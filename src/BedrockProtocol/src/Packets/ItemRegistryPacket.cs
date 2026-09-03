using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(162)]
public class ItemRegistryPacket : DataPacket {
    public ItemData[] Items = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Items.Length);
        foreach (ItemData item in Items)
            item.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        Items = new ItemData[count];
        for (int index = 0; index < Items.Length; index++) {
            ItemData item = new();
            item.Read(ref reader);
            Items[index] = item;
        }
    }
}
