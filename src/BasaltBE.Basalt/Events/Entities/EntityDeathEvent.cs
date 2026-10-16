using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Entities;

public sealed class EntityDeathEvent(Entity entity, Entity? killer = null) : Event {
    public readonly Entity Entity = entity;
    public readonly Entity? Killer = killer;
}
