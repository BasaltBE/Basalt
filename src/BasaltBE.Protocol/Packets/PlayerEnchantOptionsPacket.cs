using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(146)]
public sealed class PlayerEnchantOptionsPacket : DataPacket {
    public ItemEnchantOption[] Options = [];

    public override void Deserialize(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count > 3)
            throw new InvalidDataException("Enchanting options cannot contain more than three entries.");
        Options = new ItemEnchantOption[(int)count];
        for (int index = 0; index < Options.Length; index++) {
            Options[index] = new ItemEnchantOption();
            Options[index].Read(ref reader);
        }
    }

    public override void Serialize(ref BinaryWriter writer) {
        if (Options.Length > 3)
            throw new InvalidDataException("Enchanting options cannot contain more than three entries.");
        writer.WriteVarUInt((uint)Options.Length);
        foreach (ItemEnchantOption option in Options)
            option.Write(ref writer);
    }
}
