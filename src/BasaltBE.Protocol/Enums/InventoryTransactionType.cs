namespace BasaltBE.Protocol.Enums;

public enum InventoryTransactionType {
    Normal = 0,
    InventoryMismatch = 1,
    ItemUse = 2,
    ItemUseOnActor = 3,
    ItemRelease = 4
}
