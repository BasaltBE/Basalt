namespace Basalt.Core.Entities;

using Basalt.Protocol.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Enums;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Nbt;
using Basalt.Core.Worlds;
using Basalt.Core.Entities.Metadata;
using Basalt.Core.Item;

using Player = Player.Player;
using Basalt.Core.Traits;

using Basalt.Core.Loot;

public class Entity
{
    private static ulong _runtimeCounter;
    private readonly List<EntityTrait> _traits = [];

    public EntityType Type { get; }
    public string Identifier => Type.Identifier;
    public ulong RuntimeId { get; } = ++_runtimeCounter;
    public long UniqueId => unchecked((long)RuntimeId);
    public Vec3f Position;
    public Vec3f Location
    {
        get => Position;
        set => Position = value;
    }
    public Vec3f Velocity;
    public EntityAttributes Attributes { get; } = new();
    public EntityActorFlags Flags { get; }
    public EntityActorMetadata Metadata { get; }
    public Dimension? Dimension { get; protected set; }
    public bool AttributesDirty { get; set; }
    public bool IsAlive { get; private set; }
    public bool PendingDespawn { get; private set; }
    public bool IsSprinting
    {
        get => Flags.GetActorFlag(ActorFlag.Sprinting);
        set => Flags.SetActorFlag(ActorFlag.Sprinting, value);
    }

    public bool IsSneaking
    {
        get => Flags.GetActorFlag(ActorFlag.Sneaking);
        set => Flags.SetActorFlag(ActorFlag.Sneaking, value);
    }

    public bool IsSwimming;
    private readonly HashSet<EffectType> _effects = [];


    public Entity(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(identifier));
        }

        Type = EntityType.GetOrCreate(identifier);
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
        if (GetTrait(trait.Identifier) is not null)
        {
            return trait;
        }

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

    public void Tick(ulong currentTick, uint deltaTick)
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

        if (AttributesDirty && this is Player player)
        {
            player.SendAttributes();
        }
    }


    public virtual void Spawn(Dimension dimension, EntitySpawnOptions options)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        Dimension = dimension;
        IsAlive = true;
        PendingDespawn = false;
        dimension.AddEntity(this);
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnSpawn(options);
        }

        SetActorDataPacket actorData = CreateActorDataPacket(Dimension.World is Tickable tickable ? tickable.TickValue : 0);
        if (this is Player player)
        {
            Dimension.Broadcast(actorData, new BroadcastOptions { Except = [player] });
            return;
        }

        Dimension.Broadcast(actorData);
    }

    public void Despawn(EntityDespawnOptions options)
    {
        if (PendingDespawn)
        {
            return;
        }

        PendingDespawn = true;
        IsAlive = false;

        if (Dimension is not null)
        {
            if (this is Player player)
            {
                Dimension.Broadcast(new RemoveActorPacket
                {
                    EntityUniqueId = UniqueId
                }, new BroadcastOptions { Except = [player] });
            }
            else
            {
                Dimension.Broadcast(new RemoveActorPacket
                {
                    EntityUniqueId = UniqueId
                });
            }
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnDespawn(options);
        }
    }

    public void OnDeath(EntityDeathOptions options)
    {
        if (!IsAlive || PendingDespawn)
        {
            return;
        }

        Dimension? dimension = Dimension;
        if (!options.Cancel && dimension is not null)
        {
            List<ItemStack> drops = LootTableManager.GenerateLootFromEntity(this);
            for (int i = 0; i < drops.Count; i++)
            {
                ItemEntity drop = new(drops[i])
                {
                    Position = Position,
                    Velocity = new Vec3f
                    {
                        X = ((float)Random.Shared.NextDouble() - 0.5f) * 0.12f,
                        Y = 0.18f,
                        Z = ((float)Random.Shared.NextDouble() - 0.5f) * 0.12f
                    }
                };

                drop.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            }
        }

        IsAlive = false;
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnDeath(options);
        }
    }

    public void Kill(EntityDeathOptions options)
    {
        OnDeath(options);
        PendingDespawn = true;
    }

    internal void CompleteDespawn()
    {
        PendingDespawn = false;
        Dimension = null;
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

    public void OnContainerUpdate(Basalt.Core.Containers.Container container)
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

    // public virtual void SetSpeed(float speed = 1f)
    // {
    //     Speed = speed;
    //     float movement = BaseMovementSpeed * Speed;
    //     float underwater = BaseUnderwaterMovementSpeed * Speed;
    //     float lava = BaseLavaMovementSpeed * Speed;

    //     SetMovementAttribute(AttributeName.Movement, movement, BaseMovementSpeed);
    //     SetMovementAttribute(AttributeName.UnderwaterMovement, underwater, BaseUnderwaterMovementSpeed);
    //     SetMovementAttribute(AttributeName.LavaMovement, lava, BaseLavaMovementSpeed);
    // }

    // private void SetMovementAttribute(AttributeName name, float current, float @default)
    // {
    //     const float min = 0f;
    //     const float max = float.MaxValue;

    //     Protocol.Types.Attribute attribute = Attributes.GetAttribute(name) ?? new Protocol.Types.Attribute(min, max, current, @default, name);
    //     attribute.Min = min;
    //     attribute.Max = max;
    //     attribute.DefaultMin = min;
    //     attribute.DefaultMax = max;
    //     attribute.Default = @default;
    //     attribute.Current = current;
    //     Attributes.SetAttribute(attribute);
    // }

    public CompoundTag WriteToNbt()
    {
        CompoundTag root = new();
        root.Set("identifier", new StringTag { Value = Identifier });
        root.Set("x", new FloatTag { Value = Position.X });
        root.Set("y", new FloatTag { Value = Position.Y });
        root.Set("z", new FloatTag { Value = Position.Z });
        root.Set("sprinting", new ByteTag { Value = IsSprinting ? (sbyte)1 : (sbyte)0 });
        root.Set("swimming", new ByteTag { Value = IsSwimming ? (sbyte)1 : (sbyte)0 });

        ListTag traitsTag = new() { Name = "traits" };
        for (int i = 0; i < _traits.Count; i++)
        {
            EntityTrait trait = _traits[i];
            CompoundTag traitEntry = new();
            traitEntry.Set("id", new StringTag { Value = trait.Identifier });

            CompoundTag traitData = new();
            trait.OnWrite(root, traitData);
            traitEntry.Set("data", traitData);

            traitsTag.Values.Add(traitEntry);
        }

        root.Set("traits", traitsTag);
        return root;
    }

    public void FromNBT(CompoundTag root)
    {
        Position = new Vec3f
        {
            X = root.Get<FloatTag>("x")?.Value ?? Position.X,
            Y = root.Get<FloatTag>("y")?.Value ?? Position.Y,
            Z = root.Get<FloatTag>("z")?.Value ?? Position.Z
        };

        IsSprinting = (root.Get<ByteTag>("sprinting")?.Value ?? 0) != 0;
        IsSwimming = (root.Get<ByteTag>("swimming")?.Value ?? 0) != 0;

        ListTag? traitsTag = root.Get<ListTag>("traits");
        if (traitsTag is null)
        {
            return;
        }

        foreach (BaseTag tag in traitsTag.Values)
        {
            if (tag is not CompoundTag traitEntry)
            {
                continue;
            }

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null)
            {
                continue;
            }

            EntityTrait? trait = GetTrait(identifier);
            if (trait == null)
            {
                if (EntityTraitRegistry.RegisteredTraits.TryGetValue(identifier, out Type? traitType))
                {
                    if (Activator.CreateInstance(traitType, this) is EntityTrait newTrait)
                    {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            trait?.OnRead(root, traitData);
        }
    }


    public EntityTrait? GetTrait(string identifier)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (string.Equals(_traits[i].Identifier, identifier, StringComparison.Ordinal))
            {
                return _traits[i];
            }
        }

        return null;
    }

    public bool IsPlayer()
    {
        return string.Equals(Identifier, EntityIdentifier.Player.ToIdentifierString(), StringComparison.Ordinal);
    }

    public Vec3f GetHeadLocation()
    {
        return GetEyePosition();
    }

    public Vec3f GetPosition()
    {
        return new Vec3f
        {
            X = Position.X,
            Y = Position.Y - 1.62f,
            Z = Position.Z
        };
    }

    public Vec3f GetEyePosition()
    {
        return new Vec3f
        {
            X = Position.X,
            Y = Position.Y,
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
            Tick = Dimension.World is Tickable tickable ? tickable.TickValue : 0,
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

    public SetActorDataPacket CreateActorDataPacket(ulong tick)
    {
        List<ActorMetadataItem> metadata = Metadata.GetAll();
        metadata.Add(new ActorMetadataItem
        {
            Id = ActorDataId.Reserved0,
            Type = ActorDataType.Long,
            Value = Flags.Lower64()
        });
        metadata.Add(new ActorMetadataItem
        {
            Id = ActorDataId.Reserved092,
            Type = ActorDataType.Long,
            Value = Flags.Upper64()
        });

        return new SetActorDataPacket
        {
            RuntimeId = RuntimeId,
            Tick = tick,
            Metadata = metadata
        };
    }

    public virtual void SpawnTo(Player player, ulong tick)
    {
        player.Send(new AddActorPacket
        {
            EntityUniqueId = UniqueId,
            EntityRuntimeId = RuntimeId,
            EntityType = Identifier,
            Position = Position,
            Velocity = new Vec3f(),
            Pitch = 0,
            Yaw = 0,
            HeadYaw = 0,
            BodyYaw = 0,
            Attributes = [],
            EntityMetadata = CreateActorDataPacket(tick).Metadata,
            EntityProperties = new EntityProperties(),
            EntityLinks = []
        });
    }

    public virtual void OnPhysicsTick(ulong currentTick, bool grounded)
    {
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
            Tick = Dimension.World is Tickable tickable ? tickable.TickValue : 0,
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

    public string FormatIdentifier()
    {
        if (string.IsNullOrWhiteSpace(Identifier))
            return string.Empty;

        var name = Identifier.Contains(':') ? Identifier.Split(':')[1] : Identifier;

        return string.Join(" ", name.Split('_')
            .Select(word => char.ToUpper(word[0]) + word[1..]));
    }
}






