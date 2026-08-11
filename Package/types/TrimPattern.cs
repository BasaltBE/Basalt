using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TrimPattern {
    public string ItemName = string.Empty;
    public string PatternId = string.Empty;

    public void Read(BinaryReader reader) {
        ItemName = reader.ReadVarString();
        PatternId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(ItemName);
        writer.WriteVarString(PatternId);
    }
}
