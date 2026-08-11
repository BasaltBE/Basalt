using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackResponseContainerInfo {
    public FullContainerName FullContainerName = new();
    public List<ItemStackResponseSlotInfo> Slots = [];

    public void Read(BinaryReader reader) {
        FullContainerName.Read(reader);
        int count2 = checked((int)reader.ReadVarUInt());
        Slots = new List<ItemStackResponseSlotInfo>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            ItemStackResponseSlotInfo item2 = default!;
            ItemStackResponseSlotInfo readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            Slots.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        FullContainerName.Write(writer);
        writer.WriteVarUInt(checked((uint)Slots.Count));
        foreach (var item3 in Slots) {
            item3.Write(writer);
        }
    }
}
