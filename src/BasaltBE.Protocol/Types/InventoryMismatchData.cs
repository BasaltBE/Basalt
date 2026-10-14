using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class InventoryMismatchData : InventoryTransactionData {
    public override uint TypeId => 1;
    public InventoryTransaction Actions = new();
    public override void Read(ref BinaryReader reader) => Actions.Read(ref reader);
    public override void Write(ref BinaryWriter writer) => Actions.Write(ref writer);
}
