#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RedactableString {
    public string Unredacted = string.Empty;
    public string? Redacted;

    public void Read(BinaryReader reader) {
        Unredacted = reader.ReadVarString();
        Redacted = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Unredacted);
        writer.WriteVarString(Redacted ?? string.Empty);
    }
}
