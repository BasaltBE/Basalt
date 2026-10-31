using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Nbt.Types;
using GameRule = BasaltBE.Basalt.Worlds.Types.GameRule;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(
    Identifiers = ["minecraft:player"],
    Components = ["minecraft:health"]
)]
public sealed class EntityHealthTrait(Entity entity) : EntityTrait(entity) {
    private const float DefaultHealth = 20;
    private const string AttributeName = "minecraft:health";
    private const ulong AttackCooldownTicks = 10;
    private const ulong DeathAnimationTicks = 20;
    private const ulong SuffocationDamageInterval = 10;
    private const float SafeFallDistance = 3;
    private const float PositionEpsilon = 0.001f;
    private ulong? lastAttackTick;
    private float previousY;
    private float highestY;
    private bool falling;

    public float FallDistance { get; private set; }

    public float Current => Entity.Attributes.Get(AttributeName)?.Current ?? 0;
    public float Maximum => Entity.Attributes.Get(AttributeName)?.Maximum ?? 0;

    public override void OnAdd() {
        var component = Entity.Type?.PropertiesPayload?.Components.Health;
        double maximum = component?.Max
            ?? component?.Value?.Range?.RangeMax
            ?? component?.Value?.Value
            ?? DefaultHealth;
        double current = component?.Value?.Value
            ?? component?.Value?.Range?.RangeMax
            ?? maximum;
        if (!double.IsFinite(maximum) || maximum <= 0 || maximum > float.MaxValue ||
            !double.IsFinite(current)) {
            maximum = DefaultHealth;
            current = DefaultHealth;
        }

        Entity.Attributes.Set(
            AttributeName,
            0,
            (float)maximum,
            (float)Math.Clamp(current, 0, maximum),
            0,
            (float)maximum,
            (float)maximum
        );
    }

    public override void OnSpawn() => ResetFallDamage();

    public override void Read(CompoundTag entityTag) {
        if (entityTag.Get<FloatTag>("Health") is { } health && float.IsFinite(health.Value))
            Entity.Attributes.SetCurrent(AttributeName, Math.Clamp(health.Value, 0, Maximum));
    }

    public override void Write(CompoundTag entityTag) =>
        entityTag.Add("Health", new FloatTag { Value = Current });

    public override void OnRespawn() => ResetFallDamage();

    public override void Tick() {
        if (Entity is not Player player || !player.Spawned || player.Disposed || player.Dead ||
            player.GameType is not (GameType.Survival or GameType.Adventure) ||
            player.Abilities.Get(PlayerAbility.Flying) || Entity.SimulationAge % SuffocationDamageInterval != 0)
            return;

        int blockX = (int)MathF.Floor(Entity.Position.X);
        int blockY = (int)MathF.Floor(Entity.Position.Y + Entity.HeadOffset);
        int blockZ = (int)MathF.Floor(Entity.Position.Z);
        if (player.Dimension.GetChunk(blockX >> 4, blockZ >> 4) is null)
            return;

        BlockType? block = BlockPalette.FindType(player.Dimension.GetPermutation(blockX, blockY, blockZ).Identifier);
        if (block is { Solid: true, Air: false, Liquid: false })
            ApplyDamage(1, EntityDamageCause.Suffocation);
    }

    public override void OnMove() {
        if (Entity is not Player player || player.Dead) {
            ResetFallDamage();
            return;
        }

        float currentY = Entity.Position.Y;
        float deltaY = currentY - previousY;
        if (deltaY > PositionEpsilon) {
            if (!falling || currentY > highestY)
                highestY = currentY;
            falling = false;
            FallDistance = 0;
        }
        else if (deltaY < -PositionEpsilon) {
            if (!falling) {
                highestY = previousY;
                falling = true;
            }
            FallDistance = Math.Max(0, highestY - currentY);
        }

        previousY = currentY;
        if (falling && Entity.GetTrait<EntityMovementTrait>()?.VerticalCollision == true && deltaY <= 0)
            Land(player);
    }

    public bool ApplyDamage(float amount, EntityDamageCause cause, Entity? source = null) {
        Entity.SpawnedDimension?.World?.CheckOwnership();
        if (!float.IsFinite(amount) || amount <= 0 || Entity.Dead ||
            Entity.Attributes.Get(AttributeName) is not { } health)
            return false;

        float damage = Entity.GetTrait<EntityArmorTrait>()?.ReduceDamage(amount, cause) ?? amount;
        damage = Math.Min(damage, health.Current - health.Minimum);
        if (damage <= 0)
            return false;

        ulong? attackTick = null;
        if (cause == EntityDamageCause.Entity && Entity.SpawnedDimension?.World is not null) {
            ulong currentTick = Entity.SimulationAge;
            if (lastAttackTick is ulong previousTick && currentTick >= previousTick &&
                currentTick - previousTick < AttackCooldownTicks)
                return false;
            attackTick = currentTick;
        }

        EntityHurtEvent hurt = new(Entity, source, damage, cause);
        if (Entity.SpawnedDimension?.Events is { } events && events.HasSubscribers<EntityHurtEvent>())
            events.Emit(hurt);
        if (hurt.IsCanceled())
            return false;

        float current = health.Current - damage;
        EntityDeathEvent? death = null;
        if (current <= health.Minimum) {
            death = new EntityDeathEvent(Entity, source, cause);
            if (Entity.SpawnedDimension?.Events is { } deathEvents && deathEvents.HasSubscribers<EntityDeathEvent>())
                deathEvents.Emit(death);
            if (death.IsCanceled())
                return false;
        }

        Entity.Attributes.SetCurrent(AttributeName, current);
        if (Entity is Player)
            Entity.GetTrait<PlayerHungerTrait>()?.AddExhaustion(damage * 0.1f);
        if (attackTick is ulong appliedAttackTick)
            lastAttackTick = appliedAttackTick;
        Entity.OnHurt(hurt);
        if (Entity.SpawnedDimension is { } dimension) {
            dimension.Broadcast(new ActorEventPacket {
                TargetRuntimeId = Entity.RuntimeId,
                Event = ActorEvent.Hurt,
                Data = (int)(cause == EntityDamageCause.Entity
                    ? ActorDamageCause.EntityAttack
                    : ActorDamageCause.None)
            }, Entity);
            dimension.Broadcast(new LevelSoundEventPacket {
                SoundEvent = "hurt",
                Position = Entity.Position,
                ActorIdentifier = Entity.Type?.Identifier ?? string.Empty,
                ActorUniqueId = unchecked((long)Entity.RuntimeId)
            }, Entity);
            if (cause == EntityDamageCause.Entity && source is not null) {
                dimension.Broadcast(new LevelSoundEventPacket {
                    SoundEvent = "attack",
                    Position = source.Position,
                    ActorIdentifier = source.Type?.Identifier ?? string.Empty,
                    ActorUniqueId = unchecked((long)source.RuntimeId)
                }, source);
            }
        }

        if (death is not null) {
            Entity.Dead = true;
            Entity.OnDeath(death);
            if (Entity is Player player && Entity.SpawnedDimension is { } playerDimension) {
                playerDimension.Broadcast(new ActorEventPacket {
                    TargetRuntimeId = Entity.RuntimeId,
                    Event = ActorEvent.Death
                }, Entity);
                foreach (Player viewer in playerDimension.GetPlayers())
                    viewer.GetTrait<PlayerChunkRenderingTrait>()?.RemoveEntity(Entity);
                player.SendPacket(new RespawnPacket {
                    Position = playerDimension.SpawnPosition,
                    State = PlayerRespawnState.SearchingForSpawn,
                    RuntimeId = player.RuntimeId
                });

                if (player.GameType is GameType.Survival or GameType.Adventure &&
                    playerDimension.World?.GameRules.Get(GameRule.KeepInventory) != true) {
                    player.OpenedContainer?.Close(player);
                    if (player.GetTrait<PlayerInventoryTrait>() is { } inventory) {
                        Container[] containers = [inventory.Inventory, inventory.CraftingGrid, inventory.Cursor];
                        if (player.GetTrait<EntityArmorTrait>() is { } armor)
                            containers = [.. containers, armor.Armor];

                        foreach (Container container in containers) {
                            for (int slot = 0; slot < container.ContainerSize; slot++) {
                                if (container.GetItem(slot) is { } item &&
                                    container.TakeItem(slot, item.StackSize) is { } dropped)
                                    new ItemEntity(dropped, player).Spawn(playerDimension);
                            }
                        }
                    }

                    if (player.GetTrait<PlayerExperienceTrait>() is { } experience) {
                        int droppedExperience = Math.Min(100, experience.Level * 7);
                        if (droppedExperience > 0)
                            playerDimension.World?.Server?.ExperienceDrops.Drop(
                                playerDimension,
                                player.Position,
                                droppedExperience
                            );
                        experience.TotalExperience = 0;
                        experience.SetLevels(0);
                        experience.SetPoints(0);
                    }
                }
            }
            if (Entity is not Player && Entity.SpawnedDimension is { } dropDimension &&
                dropDimension.World is { } dropWorld && Entity.Type is { } entityType) {
                foreach (EntityDrop drop in dropWorld.LootTables.GetEntityDrops(entityType.Identifier)) {
                    ItemType? itemType = ItemPalette.Find(drop.Identifier);
                    if (itemType is null || itemType.NetworkId == 0 || itemType.MaxAmount is <= 0 or > ushort.MaxValue)
                        continue;

                    int dropAmount = drop.MinAmount == drop.MaxAmount
                        ? drop.MinAmount
                        : (int)Random.Shared.NextInt64(drop.MinAmount, (long)drop.MaxAmount + 1);
                    while (dropAmount > 0) {
                        ushort stackSize = (ushort)Math.Min(itemType.MaxAmount, dropAmount);
                        ItemEntity item = new(new ItemStack(itemType, stackSize), Entity);
                        item.Spawn(dropDimension);
                        dropAmount -= stackSize;
                    }
                }

                if (source is Player && dropWorld.Server is { } server) {
                    int experience = server.ExperienceDrops.GetEntityExperience(entityType.Identifier);
                    server.ExperienceDrops.Drop(dropDimension, Entity.Position, experience);
                }
            }

            if (Entity is not Player && Entity.SpawnedDimension is { } deadDimension) {
                deadDimension.Broadcast(new ActorEventPacket {
                    TargetRuntimeId = Entity.RuntimeId,
                    Event = ActorEvent.Death
                }, Entity);
                if (deadDimension.World is not null)
                    Entity.DeathDespawnTick = Entity.SimulationAge + DeathAnimationTicks;
                else
                    deadDimension.RemoveEntity(Entity);
            }
        }
        else if (cause == EntityDamageCause.Entity && source is not null) {
            ApplyKnockback(source);
        }
        return true;
    }

    public bool Heal(float amount) {
        if (!float.IsFinite(amount) || amount <= 0 || Entity.Dead ||
            Entity.Attributes.Get(AttributeName) is not { } health ||
            health.Current >= health.Maximum)
            return false;

        return Entity.Attributes.SetCurrent(AttributeName, health.Current + amount);
    }

    internal bool Respawn() {
        if (Entity is not Player || !Entity.Dead || Entity.Attributes.Get(AttributeName) is not { } health)
            return false;

        Entity.Dead = false;
        Entity.Attributes.SetCurrent(AttributeName, health.Maximum);
        return true;
    }

    private void ApplyKnockback(Entity source) {
        if (Entity.SpawnedDimension is not { } dimension ||
            !ReferenceEquals(source.SpawnedDimension, dimension) ||
            Entity.GetTrait<EntityMovementTrait>() is not { } movement)
            return;

        SetActorMotionPacket packet = new() {
            TargetRuntimeId = Entity.RuntimeId,
            Tick = Entity is Player
                ? movement.InputTick ?? 0
                : 0
        };
        if (!movement.CalculateKnockback(source, packet.Motion))
            return;

        if (Entity is Player player) {
            dimension.Broadcast(packet, Entity, exclude: player);
            player.SendPacket(packet);
        }
        else {
            movement.Motion.X = packet.Motion.X;
            movement.Motion.Y = packet.Motion.Y;
            movement.Motion.Z = packet.Motion.Z;
            dimension.Broadcast(packet, Entity);
        }
    }

    private void Land(Player player) {
        EntityMovementTrait? movement = Entity.GetTrait<EntityMovementTrait>();
        float damageMultiplier = GetLandingDamageMultiplier(player);
        if ((player.GameType is GameType.Survival or GameType.Adventure) &&
            player.Dimension.World?.GameRules.Get(GameRule.FallDamage) != false &&
            movement?.VerticalCollision == true &&
            !player.Abilities.Get(PlayerAbility.Flying) &&
            !player.Flags.Get(ActorFlag.Swimming) &&
            damageMultiplier > 0 && FallDistance > SafeFallDistance) {
            float damage = MathF.Floor((FallDistance - SafeFallDistance) * damageMultiplier);
            if (damage > 0)
                ApplyDamage(damage, EntityDamageCause.Fall);
        }

        FallDistance = 0;
        falling = false;
        highestY = Entity.Position.Y;
    }

    private static float GetLandingDamageMultiplier(Player player) {
        int x = (int)MathF.Floor(player.Position.X);
        int y = (int)MathF.Floor(player.Position.Y - PositionEpsilon);
        int z = (int)MathF.Floor(player.Position.Z);
        if (player.Dimension.GetChunk(x >> 4, z >> 4) is null)
            return 1;

        BlockType? block = BlockPalette.FindType(player.Dimension.GetPermutation(x, y, z).Identifier);
        if (block?.Liquid == true)
            return 0;

        string? identifier = block?.Identifier;
        if (identifier is "minecraft:slime_block" or "minecraft:powder_snow")
            return 0;
        if (identifier == "minecraft:hay_block")
            return 0.2f;
        return identifier?.EndsWith("_bed", StringComparison.Ordinal) == true ? 0.5f : 1;
    }

    private void ResetFallDamage() {
        previousY = Entity.Position.Y;
        highestY = previousY;
        FallDistance = 0;
        falling = false;
    }
}
