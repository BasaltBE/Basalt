using Basalt.Commands;
using Basalt.Containers;
using Basalt.Core;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Traits;
using Basalt.World.Dimension;

namespace Basalt.Entity.Traits;

public abstract class EntityTrait : Trait
{
    public static readonly EntityIdentifier[] Types = [];
    public static readonly string[] Components = [];

    protected Entity Entity { get; }

    protected Dimension Dimension => Entity.Dimension
        ?? throw new InvalidOperationException("Entity is not in a dimension.");

    protected EntityTrait(Entity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public virtual void OnSpawn(EntitySpawnOptions details)
    {
    }

    public virtual void OnDespawn(EntityDespawnOptions details)
    {
    }

    public virtual void OnDeath(EntityDeathOptions details)
    {
    }

    public virtual void OnTeleport(EntityTeleportOptions details)
    {
    }

    public virtual void OnMove(EntityMoveOptions details)
    {
    }

    public virtual void OnInteract(global::Basalt.Core.Player player, EntityInteractMethod method)
    {
    }

    public virtual bool OnCommand(CommandExecutionState state)
    {
        return true;
    }

    public virtual void OnContainerUpdate(Container container)
    {
    }

    public virtual void OnFallOnBlock(EntityFallOnBlockTraitEvent @event)
    {
    }

    public virtual void OnRendered(EntityRenderedOptions details)
    {
    }

    public abstract EntityTrait Clone(Entity entity);

    public override Trait Clone(params object?[] args)
    {
        if (args.Length != 1 || args[0] is not Entity entity)
        {
            throw new ArgumentException("EntityTrait.Clone requires exactly one Entity argument.", nameof(args));
        }

        return Clone(entity);
    }
}
