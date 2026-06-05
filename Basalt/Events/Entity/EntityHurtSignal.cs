namespace Basalt.Core.Events;

using Basalt.Protocol.Enums;
using Entity = Entities.Entity;

public sealed class EntityHurtSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntityHurt;
    public global::Basalt.Core.Entities.Entity Entity { get; }
    public float Amount;
    public ActorDamageCause? Cause { get; }
    public global::Basalt.Core.Entities.Entity? Damager { get; }
    public bool Cancelled;

    public EntityHurtSignal(Entity entity, float amount, ActorDamageCause? cause, Entity? damager)
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






