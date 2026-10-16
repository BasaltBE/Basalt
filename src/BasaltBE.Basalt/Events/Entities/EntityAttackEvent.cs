using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Entities;

public sealed class EntityAttackEvent(Entity attacker, Entity target, ItemStack? item) : Event {
    public readonly Entity Attacker = attacker;
    public readonly Entity Target = target;
    public readonly ItemStack? Item = item;
}
