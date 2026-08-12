#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PackedItemUseLegacyInventoryTransaction {
    public ItemStackLegacyRequestId LegacyRequestID = new();
    public List<LegacySetSlot>? LegacySetItemSlots;
    public ItemUseInventoryTransaction ItemUseTransaction = new();

    public void Read(BinaryReader reader) {
        LegacyRequestID.Read(reader);
        if (reader.ReadBool()) {
            int count2 = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new List<LegacySetSlot>(count2);
            for (int i2 = 0; i2 < count2; i2++) {
                LegacySetSlot item2 = default!;
                LegacySetSlot readValue1002 = new();
                readValue1002.Read(reader);
                item2 = readValue1002;
                LegacySetItemSlots.Add(item2);
            }
        } else {
            LegacySetItemSlots = default;
        }
        ItemUseTransaction.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        LegacyRequestID.Write(writer);
        writer.WriteBool(LegacySetItemSlots is not null);
        if (LegacySetItemSlots is { } optionalValue3) {
            writer.WriteVarUInt(checked((uint)optionalValue3.Count));
            foreach (var item3 in optionalValue3) {
                item3.Write(writer);
            }
        }
        ItemUseTransaction.Write(writer);
    }
}
