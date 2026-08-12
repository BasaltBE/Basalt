#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeOverworldGenRulesData {
    public List<BiomeWeightedData> HillsTransformations = [];
    public List<BiomeWeightedData> MutateTransformations = [];
    public List<BiomeWeightedData> RiverTransformations = [];
    public List<BiomeWeightedData> ShoreTransformations = [];
    public List<BiomeConditionalTransformationData> PreHillsEdge = [];
    public List<BiomeConditionalTransformationData> PostShoreEdge = [];
    public List<BiomeWeightedTemperatureData> Climate = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        HillsTransformations = new List<BiomeWeightedData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeWeightedData item0 = default!;
            BiomeWeightedData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            HillsTransformations.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        MutateTransformations = new List<BiomeWeightedData>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            BiomeWeightedData item2 = default!;
            BiomeWeightedData readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            MutateTransformations.Add(item2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        RiverTransformations = new List<BiomeWeightedData>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            BiomeWeightedData item4 = default!;
            BiomeWeightedData readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            RiverTransformations.Add(item4);
        }
        int count6 = checked((int)reader.ReadVarUInt());
        ShoreTransformations = new List<BiomeWeightedData>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            BiomeWeightedData item6 = default!;
            BiomeWeightedData readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            ShoreTransformations.Add(item6);
        }
        int count8 = checked((int)reader.ReadVarUInt());
        PreHillsEdge = new List<BiomeConditionalTransformationData>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            BiomeConditionalTransformationData item8 = default!;
            BiomeConditionalTransformationData readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            PreHillsEdge.Add(item8);
        }
        int count10 = checked((int)reader.ReadVarUInt());
        PostShoreEdge = new List<BiomeConditionalTransformationData>(count10);
        for (int i10 = 0; i10 < count10; i10++) {
            BiomeConditionalTransformationData item10 = default!;
            BiomeConditionalTransformationData readValue1010 = new();
            readValue1010.Read(reader);
            item10 = readValue1010;
            PostShoreEdge.Add(item10);
        }
        int count12 = checked((int)reader.ReadVarUInt());
        Climate = new List<BiomeWeightedTemperatureData>(count12);
        for (int i12 = 0; i12 < count12; i12++) {
            BiomeWeightedTemperatureData item12 = default!;
            BiomeWeightedTemperatureData readValue1012 = new();
            readValue1012.Read(reader);
            item12 = readValue1012;
            Climate.Add(item12);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)HillsTransformations.Count));
        foreach (var item1 in HillsTransformations) {
            item1.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)MutateTransformations.Count));
        foreach (var item3 in MutateTransformations) {
            item3.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)RiverTransformations.Count));
        foreach (var item5 in RiverTransformations) {
            item5.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)ShoreTransformations.Count));
        foreach (var item7 in ShoreTransformations) {
            item7.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)PreHillsEdge.Count));
        foreach (var item9 in PreHillsEdge) {
            item9.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)PostShoreEdge.Count));
        foreach (var item11 in PostShoreEdge) {
            item11.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)Climate.Count));
        foreach (var item13 in Climate) {
            item13.Write(writer);
        }
    }
}
