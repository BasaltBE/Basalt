using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemEnchants {
    public int Slot;
    public List<List<EnchantmentInstance>> Value = [];

    public void Read(BinaryReader reader) {
        Slot = reader.ReadInt32(true);
        int count2 = 3;
        Value = new List<List<EnchantmentInstance>>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            List<EnchantmentInstance> item2 = default!;
            int count1002 = checked((int)reader.ReadVarUInt());
            item2 = new List<EnchantmentInstance>(count1002);
            for (int i1002 = 0; i1002 < count1002; i1002++) {
                EnchantmentInstance item1002 = default!;
                EnchantmentInstance readValue2002 = new();
                readValue2002.Read(reader);
                item1002 = readValue2002;
                item2.Add(item1002);
            }
            Value.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(Slot, true);
        foreach (var item3 in Value) {
            writer.WriteVarUInt(checked((uint)item3.Count));
            foreach (var item1003 in item3) {
                item1003.Write(writer);
            }
        }
    }
}
