using Basalt.Entity;
using Basalt.Entity.Traits.Types;

namespace Basalt.Events;

public sealed class EntityDieSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntityDie;
    public global::Basalt.Entity.Entity Entity { get; }
    public EntityDeathOptions Options;

    public EntityDieSignal(global::Basalt.Entity.Entity entity, EntityDeathOptions options)
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
