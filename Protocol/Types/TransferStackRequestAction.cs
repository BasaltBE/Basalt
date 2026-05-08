using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TransferStackRequestAction(byte type) : IStackRequestAction, DataType
{
    public byte ActionType => type;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();
    public StackRequestSlotInfo Destination { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(ref reader);
        Destination.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(ref writer);
        Destination.Write(ref writer);
    }
}
