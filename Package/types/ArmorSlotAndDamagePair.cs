using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ArmorSlotAndDamagePair {
    public ArmorSlot ArmorSlot;
    public short Damage;

    public void Read(BinaryReader reader) {
        ArmorSlot = (global::BedrockProtocol.Enums.ArmorSlot)reader.ReadZigZag();
        Damage = reader.ReadInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)ArmorSlot);
        writer.WriteInt16(Damage, true);
    }
}
