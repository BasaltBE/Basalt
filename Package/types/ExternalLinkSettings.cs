using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ExternalLinkSettings {
    public string URL = string.Empty;
    public string DisplayName = string.Empty;

    public void Read(BinaryReader reader) {
        URL = reader.ReadVarString();
        DisplayName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(URL);
        writer.WriteVarString(DisplayName);
    }
}
