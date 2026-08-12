#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ProtocolDoubleTag {
    public double Data;

    public void Read(BinaryReader reader) {
        Data = reader.ReadF64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF64(Data, true);
    }
}
