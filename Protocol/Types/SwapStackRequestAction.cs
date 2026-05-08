using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SwapStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 2;
    public StackRequestSlotInfo Source { get; set; } = new();
    public StackRequestSlotInfo Destination { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Source.Read(ref reader);
        Destination.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        Source.Write(ref writer);
        Destination.Write(ref writer);
    }
}
