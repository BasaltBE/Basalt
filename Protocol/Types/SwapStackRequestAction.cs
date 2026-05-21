using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SwapStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 2;
    /// <summary>
    /// Source slot info.
    /// </summary>
    public StackRequestSlotInfo Source = new();
    /// <summary>
    /// Destination slot info.
    /// </summary>
    public StackRequestSlotInfo Destination = new();

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
