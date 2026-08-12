#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RotOption {
    public float X;
    public float Y;

    public void Read(BinaryReader reader) {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
    }
}
