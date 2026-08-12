#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EduSharedUriResource {
    public string ButtonName = string.Empty;
    public string LinkUri = string.Empty;

    public void Read(BinaryReader reader) {
        ButtonName = reader.ReadVarString();
        LinkUri = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(ButtonName);
        writer.WriteVarString(LinkUri);
    }
}
