#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeLegacyWorldGenRulesData {
    public List<BiomeConditionalTransformationData> LegacyPreHillsEdge = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        LegacyPreHillsEdge = new List<BiomeConditionalTransformationData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeConditionalTransformationData item0 = default!;
            BiomeConditionalTransformationData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            LegacyPreHillsEdge.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)LegacyPreHillsEdge.Count));
        foreach (var item1 in LegacyPreHillsEdge) {
            item1.Write(writer);
        }
    }
}
