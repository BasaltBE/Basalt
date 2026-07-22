using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class DropStackRequestAction : IStackRequestAction, DataType {
    /// <summary>
    /// Stack request action id.
    /// </summary>
    public byte ActionType => 3;

    /// <summary>
    /// Amount of items to drop.
    /// </summary>
    public byte Count;

    /// <summary>
    /// Source slot for the dropped items.
    /// </summary>
    public StackRequestSlotInfo Source = new();

    /// <summary>
    /// Whether the drop is randomised.
    /// </summary>
    public bool Randomly;
    public void Read(BinaryReader reader) {
        Count = reader.ReadUInt8();
        Source.Read(reader);
        Randomly = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(Count);
        Source.Write(writer);
        writer.WriteBool(Randomly);
    }
}
