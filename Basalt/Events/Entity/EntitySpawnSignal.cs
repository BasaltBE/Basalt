namespace Basalt.Server.Events;

using Basalt.Server.Entity;
using Basalt.Server.Entity.Traits.Types;

public sealed class EntitySpawnSignal : EntitySignal
{
    public override ServerEvent Event => ServerEvent.EntitySpawn;
    public Entity Entity { get; }
    public EntitySpawnOptions Options { get; }

    public EntitySpawnSignal(Entity entity, EntitySpawnOptions options)
    {
        Entity = entity;
        Options = options;
    }
}






