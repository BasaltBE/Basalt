using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class DestroyStackRequestAction(byte type) : IStackRequestAction, DataType {
    /// <summary>
    /// Stack request action id.
    /// </summary>
    public byte ActionType => type;

    /// <summary>
    /// Amount of items to destroy.
    /// </summary>
    public byte Count;

    /// <summary>
    /// Source slot for the removed items.
    /// </summary>
    public StackRequestSlotInfo Source = new();

    public void Read(BinaryReader reader) {
        Count = reader.ReadUInt8();
        Source.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Count);
        Source.Write(writer);
    }
}
