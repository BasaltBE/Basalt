using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class DropStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 3;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();
    public bool Randomly { get; set; }

    public void Read(BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(reader);
        Randomly = reader.ReadBool();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(writer);
        writer.WriteBool(Randomly);
    }
}
