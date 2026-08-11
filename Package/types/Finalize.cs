using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Finalize : BookEditOperationVariant {
    public string Title = string.Empty;
    public string Author = string.Empty;
    public string XUID = string.Empty;

    public void Read(BinaryReader reader) {
        Title = reader.ReadVarString();
        Author = reader.ReadVarString();
        XUID = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Title);
        writer.WriteVarString(Author);
        writer.WriteVarString(XUID);
    }
}
