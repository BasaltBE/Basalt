using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackRequestSlotInfo : DataType {
    /// <summary>
    /// Container this slot belongs to.
    /// </summary>
    public FullContainerName Container = new();
    /// <summary>
    /// Slot index inside the container.
    /// </summary>
    public byte Slot;
    /// <summary>
    /// Client stack network id for validation.
    /// </summary>
    public int StackNetworkId;
    public void Read(BinaryReader reader) {
        Container.Read(reader);
        Slot = reader.ReadUInt8();
        StackNetworkId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        Container.Write(writer);
        writer.WriteUInt8(Slot);
        writer.WriteZigZag(StackNetworkId);
    }
}
