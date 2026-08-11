using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeNoiseGradientSurfaceData {
    public List<uint> NonReplaceableBlocks = [];
    public List<SerializedNoiseBlockSpecifier> GradientBlocks = [];
    public NoiseDescriptor Noise = new();

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        NonReplaceableBlocks = new List<uint>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            uint item0 = default!;
            item0 = reader.ReadUInt32(true);
            NonReplaceableBlocks.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        GradientBlocks = new List<SerializedNoiseBlockSpecifier>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            SerializedNoiseBlockSpecifier item2 = default!;
            SerializedNoiseBlockSpecifier readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            GradientBlocks.Add(item2);
        }
        Noise.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)NonReplaceableBlocks.Count));
        foreach (var item1 in NonReplaceableBlocks) {
            writer.WriteUInt32(item1, true);
        }
        writer.WriteVarUInt(checked((uint)GradientBlocks.Count));
        foreach (var item3 in GradientBlocks) {
            item3.Write(writer);
        }
        Noise.Write(writer);
    }
}
