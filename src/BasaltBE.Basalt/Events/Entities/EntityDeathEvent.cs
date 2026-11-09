using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;

namespace BasaltBE.Basalt.Events.Entities;

public sealed class EntityDeathEvent(Entity entity, Entity? killer, EntityDamageCause cause) : Event {
    public readonly Entity Entity = entity;
    public readonly Entity? Killer = killer;
    public readonly EntityDamageCause Cause = cause;
}
