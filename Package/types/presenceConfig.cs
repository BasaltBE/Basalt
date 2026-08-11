using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class presenceConfig {
    public string RichPresenceId = string.Empty;

    public void Read(BinaryReader reader) {
        RichPresenceId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(RichPresenceId);
    }
}
