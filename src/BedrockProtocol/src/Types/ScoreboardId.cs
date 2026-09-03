using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ScoreboardId : DataType {
    public long Value;

    public override void Write(ref BinaryWriter writer) => writer.WriteVarLong(Value);
    public override void Read(ref BinaryReader reader) => Value = reader.ReadVarLong();
}
