using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreateStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 6;
    public byte ResultsSlot { get; set; }
    public void Read(ref BinaryReader reader) => ResultsSlot = reader.ReadUInt8();
    public void Write(ref BinaryWriter writer) => writer.WriteUInt8(ResultsSlot);
}
