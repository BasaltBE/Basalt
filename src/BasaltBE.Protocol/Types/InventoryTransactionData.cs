using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class InventoryTransactionData : DataType {
    public abstract uint TypeId { get; }

    public static InventoryTransactionData ReadTransaction(ref BinaryReader reader, uint type) {
        InventoryTransactionData transaction = type switch {
            0 => new NormalTransactionData(),
            1 => new InventoryMismatchData(),
            2 => new ItemUseInventoryTransaction(),
            3 => new ItemUseOnActorInventoryTransaction(),
            4 => new ItemReleaseInventoryTransaction(),
            _ => throw new InvalidDataException($"Unknown inventory transaction type {type}.")
        };
        transaction.Read(ref reader);
        return transaction;
    }

    public void WriteTransaction(ref BinaryWriter writer) {
        writer.WriteVarUInt(TypeId);
        Write(ref writer);
    }
}
