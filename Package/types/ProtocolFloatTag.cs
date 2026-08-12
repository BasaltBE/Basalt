#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ProtocolFloatTag {
    public float Data;

    public void Read(BinaryReader reader) {
        Data = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Data, true);
    }
}
