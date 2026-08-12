#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ExperimentToggle {
    public string Name = string.Empty;
    public bool Enabled;

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        Enabled = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(Enabled);
    }
}
