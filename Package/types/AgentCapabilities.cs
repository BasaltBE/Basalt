using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AgentCapabilities {
    public bool CanModifyBlocks;

    public void Read(BinaryReader reader) {
        CanModifyBlocks = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(CanModifyBlocks);
    }
}
