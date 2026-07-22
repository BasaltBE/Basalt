using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class MismatchInventoryTransactionData : IInventoryTransactionData {
    public InventoryTransactionType Type => InventoryTransactionType.Mismatch;

    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }
}
