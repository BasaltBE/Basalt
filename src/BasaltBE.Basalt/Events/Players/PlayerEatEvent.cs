using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerEatEvent(Player player, ItemStack item, int nutrition, float saturation) : Event {
    public readonly Player Player = player;
    public readonly ItemStack Item = item;
    public int Nutrition = nutrition;
    public float Saturation = saturation;
}
