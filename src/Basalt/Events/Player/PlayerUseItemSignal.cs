namespace Basalt.Core.Events;

using Basalt.Core.Item;
using Basalt.Core.Player;

public sealed class PlayerUseItemSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerUseItem;
    public ItemStack Item { get; }
    public bool Cancelled;

    public PlayerUseItemSignal(Player player, ItemStack item) : base(player) {
        Item = item;
    }

    public bool Emit() => !Cancelled;

    public void Cancel() {
        Cancelled = true;
    }
}
