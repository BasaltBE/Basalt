using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SwapStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 2;
    public StackRequestSlotInfo Source { get; set; } = new();
    public StackRequestSlotInfo Destination { get; set; } = new();

    public void Read(BinaryReader reader)
    {
        Source.Read(reader);
        Destination.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        Source.Write(writer);
        Destination.Write(writer);
    }
}
