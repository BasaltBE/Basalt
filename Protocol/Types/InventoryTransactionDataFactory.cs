using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public static class InventoryTransactionDataFactory {
    public static IInventoryTransactionData Create(InventoryTransactionType type) => type switch {
        InventoryTransactionType.Normal => new NormalInventoryTransactionData(),
        InventoryTransactionType.Mismatch => new MismatchInventoryTransactionData(),
        InventoryTransactionType.UseItem => new UseItemInventoryTransactionData(),
        InventoryTransactionType.UseItemOnEntity => new UseItemOnEntityInventoryTransactionData(),
        InventoryTransactionType.ReleaseItem => new ReleaseItemInventoryTransactionData(),
        _ => throw new InvalidOperationException($"Unknown inventory transaction type: {(uint)type}")
    };
}
