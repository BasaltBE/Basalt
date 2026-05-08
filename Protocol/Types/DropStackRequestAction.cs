using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class DropStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 3;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();
    public bool Randomly { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(ref reader);
        Randomly = reader.ReadBool();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(ref writer);
        writer.WriteBool(Randomly);
    }
}
