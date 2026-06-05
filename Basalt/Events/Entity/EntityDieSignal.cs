namespace Basalt.Server.Events;

using Basalt.Server.Entity;
using Basalt.Server.Entity.Traits.Types;

public sealed class EntityDieSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntityDie;
    public Entity Entity { get; }
    public EntityDeathOptions Options;

    public EntityDieSignal(Entity entity, EntityDeathOptions options)
    {
        Entity = entity;
        Options = options;
    }

    public bool Emit()
    {
        return !Options.Cancel;
    }

    public void Cancel()
    {
        Options = Options with { Cancel = true };
    }
}






