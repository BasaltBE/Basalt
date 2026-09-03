namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Commands;
using Basalt.Core.Entities.Traits.Enums;
using Basalt.Core.Entities.Traits.Types;
using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Worlds.Dimensions;
using System.Reflection;
using Player = Player.Player;
using Basalt.Core.Traits;

public abstract class EntityTrait : Trait {
    public static readonly EntityIdentifier[] Types = [];
    public static readonly string[] Components = [];
    public override string Identifier {
        get {
#pragma warning disable IL2075
            if (GetType().GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
                property.PropertyType == typeof(string) &&
                property.GetValue(null) is string identifier &&
                !string.IsNullOrWhiteSpace(identifier)) {
                return identifier;
            }

            return base.Identifier;
#pragma warning restore IL2075
        }
    }

    protected Entity Entity { get; }

    protected Dimension Dimension => Entity.Dimension
        ?? throw new InvalidOperationException("Entity is not in a dimension.");

    protected EntityTrait(Entity entity) {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public virtual void OnSpawn(EntitySpawnOptions details) {
    }

    public virtual void OnDespawn(EntityDespawnOptions details) {
    }

    public virtual void OnDeath(EntityDeathOptions details) {
    }

    public virtual void OnHurt(EntityHurtDetails details) {
    }

    public virtual void OnTeleport(EntityTeleportOptions details) {
    }

    public virtual void OnMove(EntityMoveOptions details) {
    }

    public virtual void OnInteract(Player player, EntityInteractMethod method) {
    }

    public virtual bool OnCommand(CommandContext ctx) {
        return true;
    }

    public virtual void OnContainerUpdate(Basalt.Core.Containers.Container container) {
    }

    public virtual void OnFallOnBlock(EntityFallOnBlockTraitEvent @event) {
    }

    public virtual void OnRendered(EntityRenderedOptions details) {
    }

    public virtual void OnRead(CompoundTag entityTag, CompoundTag traitTag) {
        OnRead(traitTag);
    }

    public virtual void OnWrite(CompoundTag entityTag, CompoundTag traitTag) {
        OnWrite(traitTag);
    }

    public abstract EntityTrait Clone(Entity entity);

    public override Trait Clone(params object?[] args) {
        if (args.Length != 1 || args[0] is not Entity entity) {
            throw new ArgumentException("EntityTrait.Clone requires exactly one Entity argument.", nameof(args));
        }

        return Clone(entity);
    }
}






