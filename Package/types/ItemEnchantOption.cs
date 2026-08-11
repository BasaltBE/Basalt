using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemEnchantOption {
    public byte Cost;
    public ItemEnchants Enchants = new();
    public string EnchantName = string.Empty;
    public RecipeNetId EnchantNetId = new();

    public void Read(BinaryReader reader) {
        Cost = reader.ReadUInt8();
        Enchants.Read(reader);
        EnchantName = reader.ReadVarString();
        EnchantNetId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Cost);
        Enchants.Write(writer);
        writer.WriteVarString(EnchantName);
        EnchantNetId.Write(writer);
    }
}
