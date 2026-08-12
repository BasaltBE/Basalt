#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class NoiseDescriptor {
    public string Name = string.Empty;
    public int FirstOctave;
    public List<float> Amplitudes = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        FirstOctave = reader.ReadInt32(true);
        int count4 = checked((int)reader.ReadVarUInt());
        Amplitudes = new List<float>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            float item4 = default!;
            item4 = reader.ReadF32(true);
            Amplitudes.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteInt32(FirstOctave, true);
        writer.WriteVarUInt(checked((uint)Amplitudes.Count));
        foreach (var item5 in Amplitudes) {
            writer.WriteF32(item5, true);
        }
    }
}
