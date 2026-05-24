using Basalt.Entity;
using Basalt.Entity.Traits.Types;

namespace Basalt.Events;

public sealed class EntitySpawnSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntitySpawn;
    public global::Basalt.Entity.Entity Entity { get; }
    public EntitySpawnOptions Options { get; }

    public EntitySpawnSignal(global::Basalt.Entity.Entity entity, EntitySpawnOptions options)
    {
        Entity = entity;
        Options = options;
    }
}
