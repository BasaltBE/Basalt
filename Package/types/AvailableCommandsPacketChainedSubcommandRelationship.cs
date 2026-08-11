using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketChainedSubcommandRelationship {
    public uint SubCommandFirstValue;
    public uint SubCommandSecondValue;

    public void Read(BinaryReader reader) {
        SubCommandFirstValue = reader.ReadVarUInt();
        SubCommandSecondValue = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(SubCommandFirstValue);
        writer.WriteVarUInt(SubCommandSecondValue);
    }
}
