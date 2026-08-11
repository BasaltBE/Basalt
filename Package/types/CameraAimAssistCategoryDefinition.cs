using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistCategoryDefinition {
    public string Name = string.Empty;
    public CameraAimAssistCategoryPriorities Priorities = new();

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        Priorities.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        Priorities.Write(writer);
    }
}
