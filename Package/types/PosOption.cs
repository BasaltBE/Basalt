#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PosOption {
    public Vec3 Pos = new();

    public void Read(BinaryReader reader) {
        Pos.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Pos.Write(writer);
    }
}
