using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

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
    private ulong? lastAttackTick;

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

    private void ApplyKnockback(Entity source) {
        if (Entity.SpawnedDimension is not { } dimension ||
            !ReferenceEquals(source.SpawnedDimension, dimension) ||
            Entity.GetTrait<EntityMovementTrait>() is not { } movement)
            return;

        SetActorMotionPacket packet = new() {
            TargetRuntimeId = Entity.RuntimeId,
            Tick = dimension.TickValue
        };
        if (!movement.CalculateKnockback(source, packet.Motion))
            return;

        if (Entity is Player player) {
            dimension.Broadcast(packet, Entity, exclude: player);
            player.Network.SendPacket(player, packet);
        }
        else {
            movement.Motion.X = packet.Motion.X;
            movement.Motion.Y = packet.Motion.Y;
            movement.Motion.Z = packet.Motion.Z;
            dimension.Broadcast(packet, Entity);
        }
    }
}
