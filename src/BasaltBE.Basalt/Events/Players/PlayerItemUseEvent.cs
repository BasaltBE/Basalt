using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerItemUseEvent(
    Player player,
    ItemStack? item,
    PlayerItemUseAction action,
    int durabilityDamage
) : Event {
    public readonly Player Player = player;
    public readonly ItemStack? Item = item;
    public readonly PlayerItemUseAction Action = action;
    public int DurabilityDamage = durabilityDamage;
}
