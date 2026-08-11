using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FeatureBinaryJsonFormat {
    public string FeatureName = string.Empty;
    public string BinaryJsonOutput = string.Empty;

    public void Read(BinaryReader reader) {
        FeatureName = reader.ReadVarString();
        BinaryJsonOutput = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(FeatureName);
        writer.WriteVarString(BinaryJsonOutput);
    }
}
