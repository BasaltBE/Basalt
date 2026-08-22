using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsChainedSubcommandData : DataType {
    public string Name = string.Empty;
    public AvailableCommandsChainedSubcommandRelationship[] Relationships = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Relationships.Length);
        for (int i = 0; i < Relationships.Length; i++) Relationships[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Relationships = new AvailableCommandsChainedSubcommandRelationship[count];
        for (int i = 0; i < count; i++) {
            Relationships[i] = new AvailableCommandsChainedSubcommandRelationship();
            Relationships[i].Read(ref reader);
        }
    }
}
