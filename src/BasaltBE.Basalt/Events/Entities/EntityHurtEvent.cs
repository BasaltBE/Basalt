using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;

namespace BasaltBE.Basalt.Events.Entities;

public sealed class EntityHurtEvent(Entity entity, Entity? source, float damage, EntityDamageCause cause) : Event
{
    public readonly Entity Entity = entity;
    public readonly Entity? Source = source;
    public readonly float Damage = damage;
    public readonly EntityDamageCause Cause = cause;
}
