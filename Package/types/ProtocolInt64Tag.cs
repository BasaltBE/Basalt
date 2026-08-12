#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ProtocolInt64Tag {
    public long Data;

    public void Read(BinaryReader reader) {
        Data = reader.ReadZigZong();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(Data);
    }
}
