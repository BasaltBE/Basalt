using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemEnchantOption : DataType {
    public byte Cost;
    public ItemEnchants Enchants = new();
    public string Name = string.Empty;
    public uint EnchantNetId;

    public override void Read(ref BinaryReader reader) {
        Cost = reader.ReadUInt8();
        Enchants.Read(ref reader);
        Name = reader.ReadVarString();
        EnchantNetId = reader.ReadVarUInt();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8(Cost);
        Enchants.Write(ref writer);
        writer.WriteVarString(Name);
        writer.WriteVarUInt(EnchantNetId);
    }
}
