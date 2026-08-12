#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeReplacementsData {
    public List<BiomeReplacementData> BiomeReplacements = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        BiomeReplacements = new List<BiomeReplacementData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeReplacementData item0 = default!;
            BiomeReplacementData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            BiomeReplacements.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)BiomeReplacements.Count));
        foreach (var item1 in BiomeReplacements) {
            item1.Write(writer);
        }
    }
}
