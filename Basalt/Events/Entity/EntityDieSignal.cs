namespace Basalt.Core.Events;

using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;

public sealed class EntityDieSignal : EntitySignal {
    public override ServerEvent Event => ServerEvent.EntityDie;
    public Entity Entity { get; }
    public EntityDeathOptions Options;
    public List<ItemStack> Drops { get; }

    public EntityDieSignal(Entity entity, EntityDeathOptions options, List<ItemStack> drops) {
        Entity = entity;
        Options = options;
        Drops = drops;
    }

    public bool Emit() {
        return !Options.Cancel;
    }

    public void Cancel() {
        Options = Options with { Cancel = true };
    }
}






