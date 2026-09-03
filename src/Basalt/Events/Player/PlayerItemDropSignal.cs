namespace Basalt.Core.Events;

using Basalt.Core.Item;
using Basalt.Core.Player;

/// <summary>
/// Emitted when a player drops an item. Cancel to prevent the drop.
/// </summary>
public sealed class PlayerItemDropSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerItemDrop;
    public ItemStack Item { get; }
    public bool Cancelled;

    public PlayerItemDropSignal(Player player, ItemStack item) : base(player) {
        Item = item;
    }

    public bool Emit() {
        return !Cancelled;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
