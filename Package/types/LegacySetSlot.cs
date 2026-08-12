#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class LegacySetSlot {
    public ContainerEnumName ContainerEnum;
    public List<byte> Slots = [];

    public void Read(BinaryReader reader) {
        ContainerEnum = (global::BedrockProtocol.Enums.ContainerEnumName)reader.ReadUInt8();
        int count2 = checked((int)reader.ReadVarUInt());
        Slots = new List<byte>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            byte item2 = default!;
            item2 = reader.ReadUInt8();
            Slots.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerEnum);
        writer.WriteVarUInt(checked((uint)Slots.Count));
        foreach (var item3 in Slots) {
            writer.WriteUInt8(item3);
        }
    }
}
