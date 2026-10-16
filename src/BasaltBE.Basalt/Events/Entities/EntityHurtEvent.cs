using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Entities;

public sealed class EntityHurtEvent(Entity entity, Entity? source, float damage) : Event {
    public readonly Entity Entity = entity;
    public readonly Entity? Source = source;
    public readonly float Damage = damage;
}
