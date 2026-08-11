using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeConsolidatedFeaturesData {
    public List<BiomeConsolidatedFeatureData> Features = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Features = new List<BiomeConsolidatedFeatureData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeConsolidatedFeatureData item0 = default!;
            BiomeConsolidatedFeatureData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Features.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Features.Count));
        foreach (var item1 in Features) {
            item1.Write(writer);
        }
    }
}
