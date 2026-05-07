namespace Basalt.Protocol.Enums;

public enum InventoryTransactionType : uint
{
    Normal = 0,
    Mismatch = 1,
    UseItem = 2,
    UseItemOnEntity = 3,
    ReleaseItem = 4
}
