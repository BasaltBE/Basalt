using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsChainedSubcommandRelationship : DataType {
    public uint FirstValue;
    public uint SecondValue;

    public override void Read(ref BinaryReader reader) {
        FirstValue = reader.ReadVarUInt();
        SecondValue = reader.ReadVarUInt();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(FirstValue);
        writer.WriteVarUInt(SecondValue);
    }
}
