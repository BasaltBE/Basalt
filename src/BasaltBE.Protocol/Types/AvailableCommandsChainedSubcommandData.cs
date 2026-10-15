using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsChainedSubcommandData : DataType {
    public string Name = string.Empty;
    public AvailableCommandsChainedSubcommandRelationship[] Values = [];

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Values = new AvailableCommandsChainedSubcommandRelationship[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Values.Length; index++) {
            AvailableCommandsChainedSubcommandRelationship value = new();
            value.Read(ref reader);
            Values[index] = value;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Values.Length);
        foreach (AvailableCommandsChainedSubcommandRelationship value in Values)
            value.Write(ref writer);
    }
}
