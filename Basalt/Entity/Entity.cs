using Basalt.Protocol.Types;
using Basalt.Entity.Traits;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Core;
using Basalt.World.Dimension;
using Basalt.Traits;
using Basalt.Containers;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Nbt;

namespace Basalt.Entity;

public class Entity
{
    private static ulong _runtimeCounter;
    private readonly List<EntityTrait> _traits = [];

    public EntityType Type { get; }
    public string Identifier => Type.Identifier;
    public ulong RuntimeId { get; } = ++_runtimeCounter;
    public long UniqueId => unchecked((long)RuntimeId);
    public Vec3f Position { get; set; }
    public EntityAttributes Attributes { get; } = new();
    public EntityActorFlags Flags { get; }
    public EntityActorMetadata Metadata { get; }
    public Dimension? Dimension { get; private set; }
    public bool IsAlive { get; private set; }
    public bool IsSprinting { get; set; }
    public bool IsSwimming { get; set; }
    public IReadOnlyList<EntityTrait> Traits => _traits;
    private readonly HashSet<EffectType> _effects = [];

    public Entity(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(identifier));
        }

        Type = EntityType.GetOrPlayer(identifier);
        Flags = new EntityActorFlags(this);
        Metadata = new EntityActorMetadata(this);
        foreach (Type traitType in Type.Traits.Values)
        {
            if (Activator.CreateInstance(traitType, this) is EntityTrait trait)
            {
                AddTrait(trait);
            }
        }
    }

    public T AddTrait<T>(T trait) where T : EntityTrait
    {
        ArgumentNullException.ThrowIfNull(trait);
        _traits.Add(trait);
        trait.OnAdd();
        return trait;
    }

    public bool RemoveTrait(EntityTrait trait)
    {
        ArgumentNullException.ThrowIfNull(trait);

        if (!_traits.Remove(trait))
        {
            return false;
        }

        trait.OnRemove();
        return true;
    }

    public T? GetTrait<T>() where T : EntityTrait
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    public bool HasTrait<T>() where T : EntityTrait
    {
        return GetTrait<T>() is not null;
    }

    public void TickTraits(ulong currentTick, uint deltaTick)
    {
        TraitOnTickDetails details = new(currentTick, deltaTick);
        for (int i = 0; i < _traits.Count; i++)
        {
            EntityTrait trait = _traits[i];
            try
            {
                trait.OnTick(details);
                if (trait.ShouldRandomTick())
                {
                    trait.OnRandomTick();
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"Trait tick failed for {Identifier} ({trait.Identifier}): {exception}");
            }
        }
    }

    public void Spawn(Dimension dimension, EntitySpawnOptions options)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        Dimension = dimension;
        IsAlive = true;
        dimension.AddEntity(this);
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnSpawn(options);
        }
    }

    public void Despawn(EntityDespawnOptions options)
    {
        Dimension?.RemoveEntity(this);
        IsAlive = false;
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnDespawn(options);
        }

        Dimension = null;
    }

    public void OnDeath(EntityDeathOptions options)
    {
        IsAlive = false;
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnDeath(options);
        }
    }

    public void Kill(EntityDeathOptions options)
    {
        OnDeath(options);
    }

    public void OnTeleport(EntityTeleportOptions options)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnTeleport(options);
        }
    }

    public void OnMove(EntityMoveOptions options)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnMove(options);
        }
    }

    public void OnInteract(Player player, EntityInteractMethod method)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnInteract(player, method);
        }
    }

    public void OnContainerUpdate(Basalt.Containers.Container container)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnContainerUpdate(container);
        }
    }

    public void OnFallOnBlock(EntityFallOnBlockTraitEvent @event)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnFallOnBlock(@event);
        }
    }

    public void OnRendered(EntityRenderedOptions options)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnRendered(options);
        }
    }

    public CompoundTag WriteToNbt()
    {
        CompoundTag root = new();
        root.Set("identifier", new StringTag { Value = Identifier });
        root.Set("x", new FloatTag { Value = Position.X });
        root.Set("y", new FloatTag { Value = Position.Y });
        root.Set("z", new FloatTag { Value = Position.Z });
        root.Set("sprinting", new ByteTag { Value = IsSprinting ? (sbyte)1 : (sbyte)0 });
        root.Set("swimming", new ByteTag { Value = IsSwimming ? (sbyte)1 : (sbyte)0 });

        CompoundTag traitsTag = new();
        for (int i = 0; i < _traits.Count; i++)
        {
            EntityTrait trait = _traits[i];
            CompoundTag traitTag = new();
            trait.OnWrite(root, traitTag);
            traitsTag.Set(trait.Identifier, traitTag);
        }

        root.Set("traits", traitsTag);
        return root;
    }

    public void ReadFromNbt(CompoundTag root)
    {
        Position = new Vec3f
        {
            X = root.Get<FloatTag>("x")?.Value ?? Position.X,
            Y = root.Get<FloatTag>("y")?.Value ?? Position.Y,
            Z = root.Get<FloatTag>("z")?.Value ?? Position.Z
        };

        IsSprinting = (root.Get<ByteTag>("sprinting")?.Value ?? 0) != 0;
        IsSwimming = (root.Get<ByteTag>("swimming")?.Value ?? 0) != 0;

        CompoundTag? traitsTag = root.Get<CompoundTag>("traits");
        if (traitsTag is null)
        {
            return;
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            EntityTrait trait = _traits[i];
            CompoundTag? traitTag = traitsTag.Get<CompoundTag>(trait.Identifier);
            if (traitTag is null)
            {
                continue;
            }

            trait.OnRead(root, traitTag);
        }
    }

    public bool IsPlayer()
    {
        return string.Equals(Identifier, EntityIdentifier.Player.ToIdentifierString(), StringComparison.Ordinal);
    }

    public Vec3f GetHeadLocation()
    {
        return new Vec3f
        {
            X = Position.X,
            Y = Position.Y + 1.62f,
            Z = Position.Z
        };
    }

    public bool HasEffect(EffectType effectType)
    {
        return _effects.Contains(effectType);
    }

    public void AddEffect(EffectType effectType)
    {
        _effects.Add(effectType);
    }

    public void RemoveEffect(EffectType effectType)
    {
        _effects.Remove(effectType);
    }

    internal void SendActorFlagsUpdate()
    {
        if (Dimension is null)
        {
            return;
        }

        SetActorDataPacket packet = new()
        {
            RuntimeId = RuntimeId,
            Tick = Dimension.World?.CurrentTick ?? 0,
            Metadata =
            [
                new ActorMetadataItem
                {
                    Id = ActorDataId.Reserved0,
                    Type = ActorDataType.Long,
                    Value = Flags.Lower64()
                },
                new ActorMetadataItem
                {
                    Id = ActorDataId.Reserved092,
                    Type = ActorDataType.Long,
                    Value = Flags.Upper64()
                }
            ]
        };

        Dimension.Broadcast(packet);
    }

    internal void SendActorMetadataUpdate(ActorDataId id, ActorDataType type, object value)
    {
        if (Dimension is null)
        {
            return;
        }

        SetActorDataPacket packet = new()
        {
            RuntimeId = RuntimeId,
            Tick = Dimension.World?.CurrentTick ?? 0,
            Metadata =
            [
                new ActorMetadataItem
                {
                    Id = id,
                    Type = type,
                    Value = value
                }
            ]
        };

        Dimension.Broadcast(packet);
    }
}
