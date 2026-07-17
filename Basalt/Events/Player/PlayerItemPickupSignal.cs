namespace Basalt.Core.Events;

using Basalt.Core.Entities;
using Basalt.Core.Item;
using Basalt.Core.Player;

/// <summary>
/// Emitted when a player picks up an item entity. Cancel to prevent the pickup.
/// </summary>
public sealed class PlayerItemPickupSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerItemPickup;
    public ItemStack Item { get; }
    public ItemEntity ItemEntity { get; }
    public bool Cancelled;

    public PlayerItemPickupSignal(Player player, ItemStack item, ItemEntity itemEntity) : base(player)
    {
        Item = item;
        ItemEntity = itemEntity;
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}
