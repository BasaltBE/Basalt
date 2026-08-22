using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class InventoryActionData : DataType {
    public InventorySourceData Source = new();
    public uint Slot;
    public NetworkItemStackDescriptor FromItem = new();
    public NetworkItemStackDescriptor ToItem = new();

    public override void Write(ref BinaryWriter writer) {
        Source.Write(ref writer);
        writer.WriteVarUInt(Slot);
        FromItem.Write(ref writer);
        ToItem.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Source.Read(ref reader);
        Slot = reader.ReadVarUInt();
        FromItem.Read(ref reader);
        ToItem.Read(ref reader);
    }
}
