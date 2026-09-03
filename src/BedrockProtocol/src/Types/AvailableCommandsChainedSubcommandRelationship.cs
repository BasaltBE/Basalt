using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsChainedSubcommandRelationship : DataType {
    public uint FirstValue;
    public uint SecondValue;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32(FirstValue, true);
        writer.WriteUInt32(SecondValue, true);
    }

    public override void Read(ref BinaryReader reader) {
        FirstValue = reader.ReadUInt32(true);
        SecondValue = reader.ReadUInt32(true);
    }
}
