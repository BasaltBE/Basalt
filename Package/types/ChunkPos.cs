#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ChunkPos {
    public int X;
    public int Z;

    public void Read(BinaryReader reader) {
        X = reader.ReadZigZag();
        Z = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(X);
        writer.WriteZigZag(Z);
    }
}
