#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketSoftEnumData {
    public string EnumName = string.Empty;
    public List<string> EnumOptions = [];

    public void Read(BinaryReader reader) {
        EnumName = reader.ReadVarString();
        int count2 = checked((int)reader.ReadVarUInt());
        EnumOptions = new List<string>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            string item2 = default!;
            item2 = reader.ReadVarString();
            EnumOptions.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(EnumName);
        writer.WriteVarUInt(checked((uint)EnumOptions.Count));
        foreach (var item3 in EnumOptions) {
            writer.WriteVarString(item3);
        }
    }
}
