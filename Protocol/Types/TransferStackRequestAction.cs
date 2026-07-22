using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TransferStackRequestAction(byte type) : IStackRequestAction, DataType {
    public byte ActionType => type;
    /// <summary>
    /// Item count to transfer.
    /// </summary>
    public byte Count;
    /// <summary>
    /// Source slot info.
    /// </summary>
    public StackRequestSlotInfo Source = new();
    /// <summary>
    /// Destination slot info.
    /// </summary>
    public StackRequestSlotInfo Destination = new();

    public void Read(BinaryReader reader) {
        Count = reader.ReadUInt8();
        Source.Read(reader);
        Destination.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Count);
        Source.Write(writer);
        Destination.Write(writer);
    }
}
