using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class InventoryMismatchData : DataType {
    public InventoryTransactionData Actions = new();

    public override void Write(ref BinaryWriter writer) => Actions.Write(ref writer);

    public override void Read(ref BinaryReader reader) => Actions.Read(ref reader);
}
