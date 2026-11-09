using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemEnchants : DataType {
    public int Slot;
    public EnchantmentInstance[][] Enchantments = [];

    public override void Read(ref BinaryReader reader) {
        Slot = reader.ReadInt32(true);
        Enchantments = new EnchantmentInstance[3][];
        for (int group = 0; group < Enchantments.Length; group++) {
            uint enchantmentCount = reader.ReadVarUInt();
            EnchantmentInstance[] enchantments = new EnchantmentInstance[(int)enchantmentCount];
            for (int index = 0; index < enchantments.Length; index++) {
                enchantments[index] = new EnchantmentInstance();
                enchantments[index].Read(ref reader);
            }
            Enchantments[group] = enchantments;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        if (Enchantments.Length != 3)
            throw new InvalidDataException("Enchanting options must contain exactly three enchantment groups.");

        writer.WriteInt32(Slot, true);
        foreach (EnchantmentInstance[] group in Enchantments) {
            writer.WriteVarUInt((uint)group.Length);
            foreach (EnchantmentInstance enchantment in group)
                enchantment.Write(ref writer);
        }
    }
}
