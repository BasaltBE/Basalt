using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeConditionalTransformationData {
    public List<BiomeWeightedData> TransformsInto = [];
    public ushort ConditionJson;
    public uint MinPassingNeighbors;

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        TransformsInto = new List<BiomeWeightedData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeWeightedData item0 = default!;
            BiomeWeightedData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            TransformsInto.Add(item0);
        }
        ConditionJson = reader.ReadUInt16(true);
        MinPassingNeighbors = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)TransformsInto.Count));
        foreach (var item1 in TransformsInto) {
            item1.Write(writer);
        }
        writer.WriteUInt16(ConditionJson, true);
        writer.WriteUInt32(MinPassingNeighbors, true);
    }
}
