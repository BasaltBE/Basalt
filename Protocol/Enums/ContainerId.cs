namespace Basalt.Protocol.Enums;

/// <summary>
/// Window-level container identifier used in inventory packets.
/// Dynamic containers are assigned IDs from First through Last.
/// </summary>
public enum ContainerId : sbyte
{
    None = -1,
    Inventory = 0,
    First = 1,
    Last = 100,
    Offhand = 119,
    Armor = 120,
    SelectionSlots = 122,
    Ui = 124,
    Registry = 125
}
