using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeReplacementData {
    public ushort ReplacementBiome;
    public ushort Dimension;
    public List<ushort> TargetBiomes = [];
    public float Amount;
    public float NoiseFrequencyScale;
    public uint ReplacementIndex;

    public void Read(BinaryReader reader) {
        ReplacementBiome = reader.ReadUInt16(true);
        Dimension = reader.ReadUInt16(true);
        int count4 = checked((int)reader.ReadVarUInt());
        TargetBiomes = new List<ushort>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            ushort item4 = default!;
            item4 = reader.ReadUInt16(true);
            TargetBiomes.Add(item4);
        }
        Amount = reader.ReadF32(true);
        NoiseFrequencyScale = reader.ReadF32(true);
        ReplacementIndex = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(ReplacementBiome, true);
        writer.WriteUInt16(Dimension, true);
        writer.WriteVarUInt(checked((uint)TargetBiomes.Count));
        foreach (var item5 in TargetBiomes) {
            writer.WriteUInt16(item5, true);
        }
        writer.WriteF32(Amount, true);
        writer.WriteF32(NoiseFrequencyScale, true);
        writer.WriteUInt32(ReplacementIndex, true);
    }
}
