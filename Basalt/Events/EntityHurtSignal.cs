using Basalt.Entity;
using Basalt.Protocol.Enums;

namespace Basalt.Events;

public sealed class EntityHurtSignal
{
    public global::Basalt.Entity.Entity Entity { get; }
    public float Amount { get; set; }
    public ActorDamageCause? Cause { get; }
    public global::Basalt.Entity.Entity? Damager { get; }
    public bool Cancelled { get; set; }

    public EntityHurtSignal(global::Basalt.Entity.Entity entity, float amount, ActorDamageCause? cause, global::Basalt.Entity.Entity? damager)
    {
        Entity = entity;
        Amount = amount;
        Cause = cause;
        Damager = damager;
    }

    public bool Emit()
    {
        return !Cancelled;
    }
}
