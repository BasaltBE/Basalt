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
    private readonly HashSet<string> _manualTraits = new(StringComparer.Ordinal);

    public EntityType Type { get; }
    public string Identifier => Type.Identifier;
    public ulong RuntimeId { get; } = ++_runtimeCounter;
    public long UniqueId => unchecked((long)RuntimeId);
    public float Speed { get; private set; } = 1f;
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
    protected virtual float BaseMovementSpeed => 0.1f;
    protected virtual float BaseUnderwaterMovementSpeed => 0.02f;
    protected virtual float BaseLavaMovementSpeed => 0.02f;

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
                AddTraitInternal(trait, false);
            }
        }
    }

    public T AddTrait<T>(T trait) where T : EntityTrait
    {
        return AddTrait(trait, true);
    }

    public T AddTrait<T>(T trait, bool manual) where T : EntityTrait
    {
        ArgumentNullException.ThrowIfNull(trait);
        AddTraitInternal(trait, manual);
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

    public virtual void SetSpeed(float speed = 1f)
    {
        Speed = speed;
        float movement = BaseMovementSpeed * Speed;
        float underwater = BaseUnderwaterMovementSpeed * Speed;
        float lava = BaseLavaMovementSpeed * Speed;

        SetMovementAttribute(AttributeName.Movement, movement, BaseMovementSpeed);
        SetMovementAttribute(AttributeName.UnderwaterMovement, underwater, BaseUnderwaterMovementSpeed);
        SetMovementAttribute(AttributeName.LavaMovement, lava, BaseLavaMovementSpeed);
    }

    private void SetMovementAttribute(AttributeName name, float current, float @default)
    {
        const float min = 0f;
        const float max = float.MaxValue;

        Protocol.Types.Attribute attribute = Attributes.GetAttribute(name) ?? new Protocol.Types.Attribute(min, max, current, @default, name);
        attribute.Min = min;
        attribute.Max = max;
        attribute.DefaultMin = min;
        attribute.DefaultMax = max;
        attribute.Default = @default;
        attribute.Current = current;
        Attributes.SetAttribute(attribute);
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
        if (_manualTraits.Count > 0)
        {
            ListTag manualTraitsTag = new() { Name = "manual_traits" };
            foreach (string identifier in _manualTraits.OrderBy(static x => x, StringComparer.Ordinal))
            {
                manualTraitsTag.Values.Add(new StringTag { Value = identifier });
            }

            root.Set("manual_traits", manualTraitsTag);
        }
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

        ListTag? manualTraitsTag = root.Get<ListTag>("manual_traits");
        if (manualTraitsTag is not null)
        {
            for (int i = 0; i < manualTraitsTag.Values.Count; i++)
            {
                if (manualTraitsTag.Values[i] is not StringTag traitIdentifierTag || string.IsNullOrWhiteSpace(traitIdentifierTag.Value))
                {
                    continue;
                }

                if (HasTraitIdentifier(traitIdentifierTag.Value))
                {
                    _manualTraits.Add(traitIdentifierTag.Value);
                    continue;
                }

                if (!EntityTraitRegistry.RegisteredTraits.TryGetValue(traitIdentifierTag.Value, out Type? traitType))
                {
                    continue;
                }

                if (Activator.CreateInstance(traitType, this) is EntityTrait trait)
                {
                    AddTraitInternal(trait, true);
                }
            }
        }

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

    private void AddTraitInternal(EntityTrait trait, bool manual)
    {
        string identifier = trait.Identifier;
        if (HasTraitIdentifier(identifier))
        {
            if (manual)
            {
                _manualTraits.Add(identifier);
            }

            return;
        }

        _traits.Add(trait);
        if (manual)
        {
            _manualTraits.Add(identifier);
        }
        trait.OnAdd();
    }

    private bool HasTraitIdentifier(string identifier)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (string.Equals(_traits[i].Identifier, identifier, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
