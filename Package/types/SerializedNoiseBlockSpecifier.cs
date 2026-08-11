using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedNoiseBlockSpecifier {
    public string Noise = string.Empty;
    public float Threshold;
    public FloatRange Range = new();
    public uint Block;

    public void Read(BinaryReader reader) {
        Noise = reader.ReadVarString();
        Threshold = reader.ReadF32(true);
        Range.Read(reader);
        Block = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Noise);
        writer.WriteF32(Threshold, true);
        Range.Write(writer);
        writer.WriteUInt32(Block, true);
    }
}
