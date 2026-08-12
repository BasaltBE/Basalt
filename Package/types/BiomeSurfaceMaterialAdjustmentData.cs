#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeSurfaceMaterialAdjustmentData {
    public List<BiomeElementData> Adjustments = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Adjustments = new List<BiomeElementData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeElementData item0 = default!;
            BiomeElementData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Adjustments.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Adjustments.Count));
        foreach (var item1 in Adjustments) {
            item1.Write(writer);
        }
    }
}
