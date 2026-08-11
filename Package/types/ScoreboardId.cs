using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ScoreboardId {
    public long Value;

    public void Read(BinaryReader reader) {
        Value = reader.ReadZigZong();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZong(Value);
    }
}
