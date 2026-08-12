#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EducationLocalLevelSettings {
    public string CodeBuilderOverrideUri = string.Empty;

    public void Read(BinaryReader reader) {
        CodeBuilderOverrideUri = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(CodeBuilderOverrideUri);
    }
}
