namespace Basalt.BedrockProtocol.Enums;

public enum InventoryTransactionType : uint {
    Normal,
    InventoryMismatch,
    ItemUse,
    ItemUseOnActor,
    ItemRelease
}
