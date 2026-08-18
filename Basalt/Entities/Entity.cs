namespace Basalt.Core.Entities;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Enums;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Profiling;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds;
using Basalt.Core.Entities.Metadata;
using Basalt.Core.Item;
using System.Diagnostics.CodeAnalysis;

using Player = Player.Player;
using Basalt.Core.Traits;

using Basalt.Core.Loot;
using Basalt.Core.Events;

using BedrockProtocol.Types;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Nbt;
using System.Security.Cryptography.X509Certificates;
using Basalt.Core.Enums;

public class Entity {
    private static long _runtimeCounter;
    private readonly List<EntityTrait> _traits = [];
    private long _uniqueId;

    public EntityType Type { get; }
    public string Identifier => Type.Identifier;
    public ulong RuntimeId { get; }
    public long UniqueId => _uniqueId;
    public Vec3 Position = new();
    public Vec3 Location {
        get => GetPosition();
        set => Position = value;
    }
    public Vec3 Velocity = new();
    public Vec3 Rotation = new();
    public EntityAttributes Attributes { get; }
    public EntityActorFlags Flags { get; }
    public EntityActorMetadata Metadata { get; }
    public Dimension? Dimension { get; protected set; }
    public bool AttributesDirty { get; set; }
    public bool IsAlive { get; private set; }
    public bool PendingDespawn { get; private set; }
    public int OnFireTicks { get; private set; }
    public bool IsInWater { get; internal set; }
    internal ulong NextVoidDamageTick;
    public bool IsSprinting {
        get => Flags.GetActorFlag(ActorFlag.Sprinting);
        set => Flags.SetActorFlag(ActorFlag.Sprinting, value);
    }

    public bool IsSneaking {
        get => Flags.GetActorFlag(ActorFlag.Sneaking);
        set => Flags.SetActorFlag(ActorFlag.Sneaking, value);
    }

    public bool IsSwimming;
    private readonly HashSet<EffectType> _effects = [];


    public Entity(string identifier) {
        if (string.IsNullOrWhiteSpace(identifier)) {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(identifier));
        }

        RuntimeId = unchecked((ulong)Interlocked.Increment(ref _runtimeCounter));
        _uniqueId = unchecked((long)RuntimeId);
        Type = EntityType.GetOrCreate(identifier);
        Attributes = new EntityAttributes(this);
        Flags = new EntityActorFlags(this);
        Metadata = new EntityActorMetadata(this);
        InitializeTraits();
    }

    private void InitializeTraits() {
        foreach (System.Type traitType in Type.Traits.Values) {
            EntityTrait? trait = CreateTrait(traitType);

            if (trait is not null) {
                AddTrait(trait);
            }
        }
    }

    private EntityTrait? CreateTrait(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        System.Type traitType
    ) {
        return Activator.CreateInstance(traitType, this) as EntityTrait;
    }

    public T AddTrait<T>(T trait) where T : EntityTrait {
        ArgumentNullException.ThrowIfNull(trait);
        if (GetTrait(trait.Identifier) is not null) {
            return trait;
        }

        _traits.Add(trait);
        trait.OnAdd();
        return trait;
    }

    public bool RemoveTrait(EntityTrait trait) {
        ArgumentNullException.ThrowIfNull(trait);

        if (!_traits.Remove(trait)) {
            return false;
        }

        trait.OnRemove();
        return true;
    }

    public T? GetTrait<T>() where T : EntityTrait {
        for (int i = 0; i < _traits.Count; i++) {
            if (_traits[i] is T typed) {
                return typed;
            }
        }

        return null;
    }

    public bool HasTrait<T>() where T : EntityTrait {
        return GetTrait<T>() is not null;
    }

    public void Tick(ulong currentTick, uint deltaTick) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone($"Entity.Tick({Identifier})") : default;
        if (OnFireTicks > 0) {
            if (HasEffect(EffectType.FireResistance) || IsInWater) {
                SetOnFire(0);
            }
            else {
                OnFireTicks = Math.Max(0, OnFireTicks - (int)deltaTick);
                if (OnFireTicks % 20 == 0) {
                    GetTrait<EntityHealthTrait>()?.ApplyDamage(1f, null, ActorDamageCause.FireTick);
                }

                Flags.SetActorFlag(ActorFlag.OnFire, OnFireTicks > 0);
            }
        }

        TraitOnTickDetails details = new(currentTick, deltaTick);
        for (int i = 0; i < _traits.Count; i++) {
            EntityTrait trait = _traits[i];
            try {
                trait.OnTick(details);
                if (trait.ShouldRandomTick()) {
                    trait.OnRandomTick();
                }
            }
            catch (Exception exception) {
                Logger.Warn($"Trait tick failed for {Identifier} ({trait.Identifier}): {exception}");
            }
        }

        if (AttributesDirty && this is Player player) {
            player.Attributes.Send();
        }
    }


    public virtual void Spawn(Dimension dimension, EntitySpawnOptions options) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Entity.Spawn") : default;
        ArgumentNullException.ThrowIfNull(dimension);

        IsAlive = true;
        PendingDespawn = false;
        NextVoidDamageTick = 0;

        if (Dimension != dimension) {
            Dimension = dimension;
            dimension.AddEntity(this);
        }

        using (Profiler.Enabled ? Profiler.BeginZone($"Spawn.Traits:{GetType().Name}") : default) {
            for (int i = 0; i < _traits.Count; i++) {
                _traits[i].OnSpawn(options);
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Entity.Spawn.ActorData") : default) {
            SetActorDataPacket actorData = CreateActorDataPacket(Dimension.World is Tickable tickable ? tickable.TickValue : 0);
            if (this is Player player && player.Xuid.Length > 0) {
                Dimension.Broadcast(actorData, new BroadcastOptions { Except = [player] });
                return;
            }

            if (this is Player) {
                return;
            }

            Dimension.Broadcast(actorData);
        }
    }

    public virtual void Despawn(EntityDespawnOptions options) {
        if (PendingDespawn) {
            return;
        }

        PendingDespawn = true;
        IsAlive = false;

        if (Dimension is not null) {
            Dimension.HideEntity(this);
        }

        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnDespawn(options);
        }
    }

    public void OnDeath(EntityDeathOptions options) {
        if (!IsAlive || PendingDespawn) {
            return;
        }

        Dimension? dimension = Dimension;
        List<ItemStack> drops = dimension is null ? [] : LootTableManager.GenerateLootFromEntity(this);
        if (dimension?.World?.Server is Server server) {
            EntityDieSignal signal = new(this, options, drops);
            server.Emit(signal);
            options = signal.Options;
            drops = signal.Drops;
        }

        if (!options.Cancel && dimension is not null) {
            ulong currentTick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
            for (int i = 0; i < drops.Count; i++) {
                ItemEntity drop = new(drops[i]) {
                    Position = Position,
                    Velocity = new Vec3 {
                        X = ((float)Random.Shared.NextDouble() - 0.5f) * 0.12f,
                        Y = 0.18f,
                        Z = ((float)Random.Shared.NextDouble() - 0.5f) * 0.12f
                    }
                };

                drop.LockPickupUntil(currentTick + 10);
                drop.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            }
        }

        IsAlive = false;
        if (dimension is not null) {
            dimension.Broadcast(new ActorEventPacket {
                TargetRuntimeID = new ActorRuntimeID() { Value = RuntimeId },
                EventID = ActorEvent.DEATH,
                Data = 0
            });
        }

        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnDeath(options);
        }
    }

    public void Kill(EntityDeathOptions options) {
        OnDeath(options);
        PendingDespawn = true;
    }

    internal void CompleteDespawn() {
        PendingDespawn = false;
        Dimension = null;
    }

    public void OnTeleport(EntityTeleportOptions options) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnTeleport(options);
        }
        Dimension?.UpdateEntityStorage(this);
        Dimension?.UpdateEntityVisibility(this);
    }

    public void OnMove(EntityMoveOptions options) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnMove(options);
        }
        Dimension?.UpdateEntityStorage(this);
        Dimension?.UpdateEntityVisibility(this);
    }

    public void OnInteract(Player player, EntityInteractMethod method) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnInteract(player, method);
        }
    }

    public void OnContainerUpdate(Basalt.Core.Containers.Container container) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnContainerUpdate(container);
        }
    }

    public void OnFallOnBlock(EntityFallOnBlockTraitEvent @event) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnFallOnBlock(@event);
        }
    }

    public void OnRendered(EntityRenderedOptions options) {
        for (int i = 0; i < _traits.Count; i++) {
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

    public virtual CompoundTag Write() {
        CompoundTag root = new();
        root.Set("basalt_entity", new ByteTag { Value = 1 });
        root.Set("identifier", new StringTag { Value = Identifier });
        root.Set("x", new FloatTag { Value = Position.X });
        root.Set("y", new FloatTag { Value = Position.Y });
        root.Set("z", new FloatTag { Value = Position.Z });
        root.Set("velocity_x", new FloatTag { Value = Velocity.X });
        root.Set("velocity_y", new FloatTag { Value = Velocity.Y });
        root.Set("velocity_z", new FloatTag { Value = Velocity.Z });
        root.Set("rotation_pitch", new FloatTag { Value = Rotation.X });
        root.Set("rotation_yaw", new FloatTag { Value = Rotation.Y });
        root.Set("rotation_head_yaw", new FloatTag { Value = Rotation.Z });
        root.Set("UniqueID", new LongTag { Value = UniqueId });

        ListTag position = new() { Name = "Pos" };
        position.Values.Add(new FloatTag { Value = Position.X });
        position.Values.Add(new FloatTag { Value = Position.Y });
        position.Values.Add(new FloatTag { Value = Position.Z });
        root.Set("Pos", position);

        ListTag motion = new() { Name = "Motion" };
        motion.Values.Add(new FloatTag { Value = Velocity.X });
        motion.Values.Add(new FloatTag { Value = Velocity.Y });
        motion.Values.Add(new FloatTag { Value = Velocity.Z });
        root.Set("Motion", motion);
        root.Set("sprinting", new ByteTag { Value = IsSprinting ? (sbyte)1 : (sbyte)0 });
        root.Set("swimming", new ByteTag { Value = IsSwimming ? (sbyte)1 : (sbyte)0 });
        root.Set("fire_ticks", new IntTag { Value = OnFireTicks });

        ListTag traitsTag = new() { Name = "traits" };
        for (int i = 0; i < _traits.Count; i++) {
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

    public virtual void Read(CompoundTag root) {
        Position = new Vec3 {
            X = root.Get<FloatTag>("x")?.Value ?? Position.X,
            Y = root.Get<FloatTag>("y")?.Value ?? Position.Y,
            Z = root.Get<FloatTag>("z")?.Value ?? Position.Z
        };
        Position = ReadVector(root, "Pos", Position);

        Velocity = new Vec3 {
            X = root.Get<FloatTag>("velocity_x")?.Value ?? Velocity.X,
            Y = root.Get<FloatTag>("velocity_y")?.Value ?? Velocity.Y,
            Z = root.Get<FloatTag>("velocity_z")?.Value ?? Velocity.Z
        };
        Rotation = new Vec3 {
            X = root.Get<FloatTag>("rotation_pitch")?.Value ?? Rotation.X,
            Y = root.Get<FloatTag>("rotation_yaw")?.Value ?? Rotation.Y,
            Z = root.Get<FloatTag>("rotation_head_yaw")?.Value ?? Rotation.Z
        };
        Velocity = ReadVector(root, "Motion", Velocity);

        IsSprinting = (root.Get<ByteTag>("sprinting")?.Value ?? 0) != 0;
        IsSwimming = (root.Get<ByteTag>("swimming")?.Value ?? 0) != 0;
        OnFireTicks = root.Get<IntTag>("fire_ticks")?.Value ?? OnFireTicks;
        Flags.SetActorFlag(ActorFlag.OnFire, OnFireTicks > 0);

        ListTag? traitsTag = root.Get<ListTag>("traits");
        if (traitsTag is null) {
            return;
        }

        foreach (BaseTag tag in traitsTag.Values) {
            if (tag is not CompoundTag traitEntry) {
                continue;
            }

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null) {
                continue;
            }

            EntityTrait? trait = GetTrait(identifier);
            if (trait == null) {
                if (EntityTraitRegistry.RegisteredTraits.TryGetValue(identifier, out System.Type? traitType)) {
                    if (Activator.CreateInstance(traitType, this) is EntityTrait newTrait) {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            trait?.OnRead(root, traitData);
        }
    }

    private static Vec3 ReadVector(CompoundTag root, string name, Vec3 fallback) {
        ListTag? values = root.Get<ListTag>(name);
        if (values is not { Values.Count: >= 3 }) {
            return fallback;
        }

        return new Vec3 {
            X = values.Values[0] switch {
                FloatTag value => value.Value,
                DoubleTag value => (float)value.Value,
                _ => fallback.X
            },
            Y = values.Values[1] switch {
                FloatTag value => value.Value,
                DoubleTag value => (float)value.Value,
                _ => fallback.Y
            },
            Z = values.Values[2] switch {
                FloatTag value => value.Value,
                DoubleTag value => (float)value.Value,
                _ => fallback.Z
            }
        };
    }

    internal void RestoreUniqueId(long uniqueId) {
        _uniqueId = uniqueId;
        if (uniqueId <= 0) {
            return;
        }

        long current = Volatile.Read(ref _runtimeCounter);
        while (current < uniqueId) {
            long observed = Interlocked.CompareExchange(ref _runtimeCounter, uniqueId, current);
            if (observed == current) {
                return;
            }

            current = observed;
        }
    }


    public EntityTrait? GetTrait(string identifier) {
        for (int i = 0; i < _traits.Count; i++) {
            if (string.Equals(_traits[i].Identifier, identifier, StringComparison.Ordinal)) {
                return _traits[i];
            }
        }

        return null;
    }

    public bool IsPlayer() {
        return string.Equals(Identifier, EntityIdentifier.Player.ToIdentifierString(), StringComparison.Ordinal);
    }

    public Vec3 GetHeadLocation() {
        return GetEyePosition();
    }

    public virtual Vec3 GetPosition() => Position;

    public Vec3 GetEyePosition() {
        return new Vec3 {
            X = Position.X,
            Y = this is Player ? Position.Y + 1.62f : Position.Y,
            Z = Position.Z
        };
    }

    public bool HasEffect(EffectType effectType) {
        return _effects.Contains(effectType);
    }

    public void AddEffect(EffectType effectType) {
        _effects.Add(effectType);
    }

    public void RemoveEffect(EffectType effectType) {
        _effects.Remove(effectType);
    }

    public void OnHurt(EntityHurtDetails details) {
        for (int i = 0; i < _traits.Count; i++) {
            _traits[i].OnHurt(details);
        }
    }

    public void SetOnFire(int ticks) {
        if (ticks < 0) {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        if (HasEffect(EffectType.FireResistance) || IsInWater) {
            ticks = 0;
        }

        if (ticks > OnFireTicks) {
            OnFireTicks = ticks;
        }
        else if (ticks == 0) {
            OnFireTicks = 0;
        }

        Flags.SetActorFlag(ActorFlag.OnFire, OnFireTicks > 0);
    }

    internal void SendActorFlagsUpdate() {
        if (Dimension is null) {
            return;
        }

        SetActorDataPacket packet = new() {
            TargetRuntimeID = new ActorRuntimeID() {
                Value = RuntimeId,
            },
            Tick = new PlayerInputTick() { InputTick = Dimension.World is Tickable tickable ? tickable.TickValue : 0 },
            ActorData = new SynchedActorDataList() {
                Data = new List<DataItemEntry> {
                    new DataItemEntry() {
                        ID = (uint)ActorDataId.Reserved0,
                        Payload = new DataItemInt64Payload() {
                            Type = DataItemType.Int64,
                            Value = Flags.Lower64(),
                        }
                    },
                    new DataItemEntry() {
                        ID = (uint)ActorDataId.Reserved092,
                        Payload = new DataItemInt64Payload() {
                            Type = DataItemType.Int64,
                            Value = Flags.Upper64(),
                        }
                    },
                },
            },
            SynchedProperties = new PropertySyncData() {
                FloatEntriesList = new List<PropertySyncFloatEntry>(),
                IntEntriesList = new List<PropertySyncIntEntry>(),
            },
        };

        Dimension.Broadcast(packet);
    }

    public SetActorDataPacket CreateActorDataPacket(ulong tick) {
        List<DataItemEntry> metadata = Metadata.GetAll();

        metadata.Add(
            new DataItemEntry() {
                ID = (uint)ActorDataId.Reserved0,
                Payload = new DataItemInt64Payload() {
                    Type = DataItemType.Int64,
                    Value = Flags.Lower64(),
                }
            }
        );
        metadata.Add(
            new DataItemEntry() {
                ID = (uint)ActorDataId.Reserved092,
                Payload = new DataItemInt64Payload() {
                    Type = DataItemType.Int64,
                    Value = Flags.Upper64(),
                }
            }
        );

        return new SetActorDataPacket {
            TargetRuntimeID = new ActorRuntimeID() { Value = RuntimeId },
            Tick = new PlayerInputTick() { InputTick = tick },
            ActorData = new SynchedActorDataList() {
                Data = metadata,
            },
            SynchedProperties = new PropertySyncData() {
                FloatEntriesList = new List<PropertySyncFloatEntry>(),
                IntEntriesList = new List<PropertySyncIntEntry>(),
            },
        };
    }

    public virtual void SpawnTo(Player player, ulong tick, Vec3? position = null) {
        player.Send(new AddActorPacket {
            TargetActorID = new ActorUniqueID() { Value = UniqueId, },
            TargetRuntimeID = new ActorRuntimeID() { Value = RuntimeId },
            ActorType = Identifier,
            Position = position ?? Position,
            Velocity = new Vec3(),
            Rotation = new Vec2() {
                X = Rotation.X,
                Y = Rotation.Y,
            },
            YBodyRotation = Rotation.Y,
            YHeadRotation = Rotation.Z,
            AttributesList = new List<SyncedAttribute> { },
            ActorData = CreateActorDataPacket(tick).ActorData,
            // EntityProperties = new EntityProperties(),
            // EntityLinks = []
            ActorLinks = new List<ActorLink>() { },
            SynchedProperties = new PropertySyncData() {
                FloatEntriesList = new List<PropertySyncFloatEntry>(),
                IntEntriesList = new List<PropertySyncIntEntry>(),
            },
        });
    }

    public void SetRotation(Vec3 rotation) {
        Rotation = rotation;
        if (Dimension is null) {
            return;
        }

        Dimension.Broadcast(new MoveActorAbsolutePacket {
            // EntityRuntimeId = RuntimeId,
            // Position = Position,
            // Rotation = rotation
            MoveData = new MoveActorAbsoluteData() {
                ActorRuntimeID = new ActorRuntimeID() { Value = RuntimeId, },
                Position = Position,
                RotationX = (byte)Rotation.X,
                RotationY = (byte)Rotation.Y,
                RotationYHead = (byte)Rotation.Z,
                Header = 0,
            }
        });
    }

    public virtual void OnPhysicsTick(ulong currentTick, bool grounded) {
    }

    internal void SendActorMetadataUpdate(
       ActorDataId id,
       DataItemEntryPayloadVariant payload
   ) {
        if (Dimension is null) {
            return;
        }

        SetActorDataPacket packet = new() {
            TargetRuntimeID = new ActorRuntimeID {
                Value = RuntimeId
            },

            Tick = new PlayerInputTick {
                InputTick = Dimension.World is Tickable tickable
                    ? tickable.TickValue
                    : 0
            },

            ActorData = new SynchedActorDataList {
                Data = [
                    new DataItemEntry {
                    ID = (uint)id,
                    Payload = payload
                }
                ]
            },

            SynchedProperties = new PropertySyncData {
                FloatEntriesList = [],
                IntEntriesList = []
            }
        };

        Dimension.Broadcast(packet);
    }

    public string FormatIdentifier() {
        if (string.IsNullOrWhiteSpace(Identifier))
            return string.Empty;

        var name = Identifier.Contains(':') ? Identifier.Split(':')[1] : Identifier;

        return string.Join(" ", name.Split('_')
            .Select(word => char.ToUpper(word[0]) + word[1..]));
    }
}






