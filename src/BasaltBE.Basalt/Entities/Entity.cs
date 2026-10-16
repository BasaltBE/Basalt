using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Attributes;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Network;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public class Entity
{
    private static long nextUniqueId = 0;
    private static long nextRuntimeId = 0;
    EntityTrait[]? traits;
    public readonly EntityAttributes Attributes = new();
    public readonly ActorFlags Flags = new();
    public readonly Vec3 Position = new();
    public readonly float HeadOffset;
    public readonly Vec2 Rotation = new();
    public float HeadRotation;

    public Entity(EntityType? type = null, float headOffset = 0)
    {
        if (!float.IsFinite(headOffset) || headOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(headOffset));

        HeadOffset = headOffset;
        Type = type;
        traits = EntityPalette.CreateTraits(this, type);
        if (traits is not null)
        {
            foreach (EntityTrait trait in traits)
                trait.OnAdd();
        }
    }

    public long UniqueId = Interlocked.Increment(ref nextUniqueId);
    public ulong RuntimeId = checked((ulong)Interlocked.Increment(ref nextRuntimeId));
    public EntityType? Type;

    /// <summary>
    /// Returns the entity's feet position. !! Not a snapshot. !!
    /// </summary>
    public Vec3 GetPosition() => Position;

    /// <summary>
    /// Returns a new head-position snapshot.
    /// </summary>
    public Vec3 GetHeadPosition() => new() { X = Position.X, Y = Position.Y + HeadOffset, Z = Position.Z };

    public void AddTrait(EntityTrait trait)
    {
        ArgumentNullException.ThrowIfNull(trait);
        if (!ReferenceEquals(trait.Entity, this))
            throw new ArgumentException("Trait belongs to a different entity.", nameof(trait));
        if (GetTrait(trait.GetType()) is not null)
            throw new InvalidOperationException($"Entity already has trait '{trait.GetType()}'.");

        if (traits is null)
        {
            traits = [trait];
        }
        else
        {
            EntityTrait[] expanded = new EntityTrait[traits.Length + 1];
            traits.CopyTo(expanded, 0);
            expanded[^1] = trait;
            traits = expanded;
        }

        trait.OnAdd();
    }

    public bool RemoveTrait<TTrait>() where TTrait : EntityTrait
    {
        if (traits is null)
            return false;

        for (int i = 0; i < traits.Length; i++)
        {
            if (traits[i] is not TTrait)
                continue;

            EntityTrait removed = traits[i];
            if (traits.Length == 1)
            {
                traits = null;
            }
            else
            {
                EntityTrait[] reduced = new EntityTrait[traits.Length - 1];
                if (i > 0)
                    Array.Copy(traits, 0, reduced, 0, i);
                if (i < reduced.Length)
                    Array.Copy(traits, i + 1, reduced, i, reduced.Length - i);
                traits = reduced;
            }

            removed.OnRemove();
            return true;
        }

        return false;
    }

    public TTrait? GetTrait<TTrait>() where TTrait : EntityTrait
    {
        if (traits is null)
            return null;

        foreach (EntityTrait trait in traits)
        {
            if (trait is TTrait match)
                return match;
        }

        return null;
    }

    public void OnSpawn() => Dispatch(static trait => trait.OnSpawn());
    public void OnDespawn() => Dispatch(static trait => trait.OnDespawn());
    public void OnDeath() => Dispatch(static trait => trait.OnDeath());
    public void OnHurt() => Dispatch(static trait => trait.OnHurt());
    public void OnMove() => Dispatch(static trait => trait.OnMove());
    public void OnAttack() => Dispatch(static trait => trait.OnAttack());

    public void OnAttack(Entity target, ItemStack? item)
    {
        if (traits is null)
            return;
        foreach (EntityTrait trait in traits)
            trait.OnAttack(target, item);
    }

    public void OnUseItem(ItemStack? item)
    {
        if (traits is null)
            return;
        foreach (EntityTrait trait in traits)
            trait.OnUseItem(item);
    }

    public void OnUseItemOnBlock(ItemStack? item, BlockPermutation block, BlockPos position, byte face, Vec3 clickPosition)
    {
        if (traits is null)
            return;
        foreach (EntityTrait trait in traits)
            trait.OnUseItemOnBlock(item, block, position, face, clickPosition);
    }

    public void OnUseItemOnEntity(ItemStack? item, Entity target, Vec3 hitPosition)
    {
        if (traits is null)
            return;
        foreach (EntityTrait trait in traits)
            trait.OnUseItemOnEntity(item, target, hitPosition);
    }

    public void Tick()
    {
        Dispatch(static trait => trait.Tick());
        if (this is not Player player || !player.Spawned || player.Disposed)
            return;

        ulong tick = player.Network.Server.TickValue;
        if (Attributes.CreateUpdate(RuntimeId, tick: tick) is { } attributes)
            player.Dimension.Broadcast(attributes, entity: player);
        if (Flags.Dirty)
            player.Dimension.Broadcast(Flags.CreateUpdate(RuntimeId, tick), entity: player);
        bool permissionsChanged = Volatile.Read(ref player.Permissions.Dirty);
        if (permissionsChanged)
            Volatile.Write(ref player.Permissions.Dirty, false);
        if (player.Abilities.Dirty || permissionsChanged)
            player.Dimension.Broadcast(player.Abilities.CreateUpdate(UniqueId, player.Permissions), entity: player);
        if (permissionsChanged)
        {
            var commands = player.Network.Server.Commands.GetCatalog(player.Permissions);
            player.Network.SendSerializedPacket(player, commands.Packet, commands.Payload);
        }
    }

    public void SetFlag(ActorFlag flag, bool enabled)
    {
        Flags.Set(flag, enabled);
    }

    public void Read(CompoundTag entityTag)
    {
        ArgumentNullException.ThrowIfNull(entityTag);
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            trait.Read(entityTag);
    }

    public void Write(CompoundTag entityTag)
    {
        ArgumentNullException.ThrowIfNull(entityTag);
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            trait.Write(entityTag);
    }

    void Dispatch(Action<EntityTrait> callback)
    {
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            callback(trait);
    }

    EntityTrait? GetTrait(Type traitType)
    {
        if (traits is null)
            return null;

        foreach (EntityTrait trait in traits)
        {
            if (trait.GetType() == traitType)
                return trait;
        }

        return null;
    }
}
