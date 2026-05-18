using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class DestroyStackRequestAction(byte type) : IStackRequestAction, DataType
{
    public byte ActionType => type;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();

    public void Read(BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(writer);
    }
}
