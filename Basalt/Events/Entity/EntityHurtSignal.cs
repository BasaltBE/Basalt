using Basalt.Entity;
using Basalt.Protocol.Enums;

namespace Basalt.Events;

public sealed class EntityHurtSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntityHurt;
    public global::Basalt.Entity.Entity Entity { get; }
    public float Amount;
    public ActorDamageCause? Cause { get; }
    public global::Basalt.Entity.Entity? Damager { get; }
    public bool Cancelled;

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

    public void Cancel()
    {
        Cancelled = true;
    }
}
