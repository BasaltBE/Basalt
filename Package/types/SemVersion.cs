#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SemVersion {
    public string Version = string.Empty;

    public void Read(BinaryReader reader) {
        Version = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Version);
    }
}
