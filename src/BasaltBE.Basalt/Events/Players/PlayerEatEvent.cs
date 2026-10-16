using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerEatEvent(Player player, ItemStack item) : Event
{
    public readonly Player Player = player;
    public readonly ItemStack Item = item;
}
