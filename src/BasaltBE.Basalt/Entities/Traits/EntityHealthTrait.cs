using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(
    Identifiers = ["minecraft:player"],
    Components = ["minecraft:health"]
)]
public sealed class EntityHealthTrait(Entity entity) : EntityTrait(entity)
{
    private const float DefaultHealth = 20;
    private const string AttributeName = "minecraft:health";
    private const ulong AttackCooldownTicks = 10;
    private const ulong DeathAnimationTicks = 20;
    private ulong? lastAttackTick;

    public float Current => Entity.Attributes.Get(AttributeName)?.Current ?? 0;
    public float Maximum => Entity.Attributes.Get(AttributeName)?.Maximum ?? 0;

    public override void OnAdd()
    {
        var component = Entity.Type?.PropertiesPayload?.Components.Health;
        double maximum = component?.Max
            ?? component?.Value?.Range?.RangeMax
            ?? component?.Value?.Value
            ?? DefaultHealth;
        double current = component?.Value?.Value
            ?? component?.Value?.Range?.RangeMax
            ?? maximum;
        if (!double.IsFinite(maximum) || maximum <= 0 || maximum > float.MaxValue ||
            !double.IsFinite(current))
        {
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

    public bool ApplyDamage(float amount, EntityDamageCause cause, Entity? source = null)
    {
        if (!float.IsFinite(amount) || amount <= 0 || Entity.Dead ||
            Entity.Attributes.Get(AttributeName) is not { } health)
            return false;

        float damage = Math.Min(amount, health.Current - health.Minimum);
        if (damage <= 0)
            return false;

        ulong? attackTick = null;
        if (cause == EntityDamageCause.Entity && Entity.SpawnedDimension?.World is { } world)
        {
            ulong currentTick = world.TickValue;
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
        if (current <= health.Minimum)
        {
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
        if (Entity.SpawnedDimension is { } dimension)
        {
            dimension.Broadcast(new ActorEventPacket
            {
                TargetRuntimeId = Entity.RuntimeId,
                Event = ActorEvent.Hurt,
                Data = (int)(cause == EntityDamageCause.Entity
                    ? ActorDamageCause.EntityAttack
                    : ActorDamageCause.None)
            }, Entity);
            dimension.Broadcast(new LevelSoundEventPacket
            {
                SoundEvent = "hurt",
                Position = Entity.Position,
                ActorIdentifier = Entity.Type?.Identifier ?? string.Empty,
                ActorUniqueId = unchecked((long)Entity.RuntimeId)
            }, Entity);
            if (cause == EntityDamageCause.Entity && source is not null)
            {
                dimension.Broadcast(new LevelSoundEventPacket
                {
                    SoundEvent = "attack",
                    Position = source.Position,
                    ActorIdentifier = source.Type?.Identifier ?? string.Empty,
                    ActorUniqueId = unchecked((long)source.RuntimeId)
                }, source);
            }
        }

        if (death is not null)
        {
            Entity.Dead = true;
            Entity.OnDeath(death);
            if (Entity is not Player && Entity.SpawnedDimension is { } deadDimension)
            {
                deadDimension.Broadcast(new ActorEventPacket
                {
                    TargetRuntimeId = Entity.RuntimeId,
                    Event = ActorEvent.Death
                }, Entity);
                if (deadDimension.World is { } deathWorld)
                    Entity.DeathDespawnTick = deathWorld.TickValue + DeathAnimationTicks;
                else
                    deadDimension.RemoveEntity(Entity);
            }
        }
        else if (cause == EntityDamageCause.Entity && source is not null)
        {
            ApplyKnockback(source);
        }
        return true;
    }

    public bool Heal(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0 || Entity.Dead ||
            Entity.Attributes.Get(AttributeName) is not { } health ||
            health.Current >= health.Maximum)
            return false;

        return Entity.Attributes.SetCurrent(AttributeName, health.Current + amount);
    }

    private void ApplyKnockback(Entity source)
    {
        if (Entity.SpawnedDimension is not { } dimension ||
            !ReferenceEquals(source.SpawnedDimension, dimension) ||
            Entity.GetTrait<EntityMovementTrait>() is not { } movement)
            return;

        SetActorMotionPacket packet = new()
        {
            TargetRuntimeId = Entity.RuntimeId,
            Tick = dimension.World?.TickValue ?? 0
        };
        if (!movement.CalculateKnockback(source, packet.Motion))
            return;

        if (Entity is Player player)
        {
            dimension.Broadcast(packet, Entity, exclude: player);
            player.Network.SendPacket(player, packet);
        }
        else
        {
            movement.Motion.X = packet.Motion.X;
            movement.Motion.Y = packet.Motion.Y;
            movement.Motion.Z = packet.Motion.Z;
            dimension.Broadcast(packet, Entity);
        }
    }
}
