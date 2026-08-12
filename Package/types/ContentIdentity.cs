#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ContentIdentity {
    public string Identity = string.Empty;

    public void Read(BinaryReader reader) {
        Identity = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Identity);
    }
}
