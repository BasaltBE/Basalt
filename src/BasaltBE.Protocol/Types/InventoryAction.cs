using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class InventoryAction : DataType {
    public InventorySource Source = new();
    public uint Slot;
    public NetworkItemStackDescriptor FromItem = new();
    public NetworkItemStackDescriptor ToItem = new();

    public override void Read(ref BinaryReader reader) {
        Source.Read(ref reader);
        Slot = reader.ReadVarUInt();
        FromItem.Read(ref reader);
        ToItem.Read(ref reader);
    }

    public override void Write(ref BinaryWriter writer) {
        Source.Write(ref writer);
        writer.WriteVarUInt(Slot);
        FromItem.Write(ref writer);
        ToItem.Write(ref writer);
    }
}
