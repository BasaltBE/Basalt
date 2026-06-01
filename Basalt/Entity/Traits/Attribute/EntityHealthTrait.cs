namespace Basalt.Server.Entity.Traits.Attribute;

using Basalt.Server.Events;
using Basalt.Server.Item.Traits;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Entity = Basalt.Server.Entity.Entity;
using Basalt.Server.Entity.Traits.Types;
using Basalt.Server.Player.Traits;
using Basalt.Server.World;

public sealed class EntityHealthTrait : EntityAttributeTrait
{
    public new static string Identifier => "health";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:health"];
    private const float KnockbackHorizontalForce = 0.28f;
    private const float KnockbackVerticalForce = 0.38f;
    private const float KnockbackVerticalLimit = 0.4f;
    private const ulong KnockbackCooldownTicks = 10;
    private ulong _lastKnockbackTick;

    public override AttributeName Attribute => AttributeName.Health;

    public EntityHealthTrait(Entity entity) : base(entity)
    {
    }

    public void ApplyDamage(float amount, Entity? damager = null, ActorDamageCause? cause = null)
    {
        EntityHurtSignal signal = new(Entity, amount, cause, damager);
        if (!signal.Emit())
        {
            return;
        }

        CurrentValue -= signal.Amount;
        if (signal.Cause == ActorDamageCause.EntityAttack && damager is not null && Entity.Dimension is not null && damager.Dimension == Entity.Dimension)
        {
            ulong currentTick = Entity.Dimension.World is Tickable tickable ? tickable.TickValue : 0;
            if (currentTick >= _lastKnockbackTick && currentTick - _lastKnockbackTick >= KnockbackCooldownTicks)
            {
                float x = Entity.Position.X - damager.Position.X;
                float z = Entity.Position.Z - damager.Position.Z;
                float length = MathF.Sqrt((x * x) + (z * z));
                if (length > 0.0001f)
                {
                    float invLength = 1f / length;
                    float velocityX = Entity.Velocity.X * 0.5f;
                    float velocityY = Entity.Velocity.Y * 0.5f;
                    float velocityZ = Entity.Velocity.Z * 0.5f;
                    velocityX += x * invLength * KnockbackHorizontalForce;
                    velocityY += KnockbackVerticalForce;
                    velocityZ += z * invLength * KnockbackHorizontalForce;
                    if (velocityY > KnockbackVerticalLimit)
                    {
                        velocityY = KnockbackVerticalLimit;
                    }

                    Entity.Velocity = new Vec3f
                    {
                        X = velocityX,
                        Y = velocityY,
                        Z = velocityZ
                    };
                    _lastKnockbackTick = currentTick;
                }
            }
        }
        if (Entity.Dimension is not null)
        {
            ActorEventPacket packet = new()
            {
                ActorRuntimeId = Entity.RuntimeId,
                Event = ActorEvent.Hurt,
                Data = (int)(signal.Cause ?? ActorDamageCause.None),
                FiredAt = new Optional<Vec3f>
                {
                    HasValue = true,
                    Value = Entity.Position
                }
            };
            Entity.Dimension.Broadcast(packet);
        }

        EntityEquipmentTrait? equipment = Entity.GetTrait<EntityEquipmentTrait>();
        if (equipment is not null)
        {
            for (int i = 0; i < equipment.Armor.Count; i++)
            {
                if (equipment.Armor[i] is not { } itemStack)
                {
                    continue;
                }

                ItemStackDurabilityTrait? durabilityTrait = itemStack.GetTrait<ItemStackDurabilityTrait>();
                durabilityTrait?.ProcessDamage(Entity);
            }
        }

        PlayerHungerTrait? hungerTrait = Entity.GetTrait<PlayerHungerTrait>();
        if (hungerTrait is not null)
        {
            hungerTrait.Exhaustion += 0.1f;
        }

        if (CurrentValue <= 0)
        {
            Entity.Kill(new EntityDeathOptions(KillerSource: damager, DamageCause: signal.Cause));
        }
    }

    public override void OnAdd()
    {
        EnsureAttribute(new AttributeProperties(0, 20, 20, 20));
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (details.InitialSpawn)
        {
            return;
        }

        CurrentValue = DefaultValue;
    }

    public override void OnDespawn(EntityDespawnOptions details)
    {
        if (details.Disconnected && CurrentValue <= MinimumValue)
        {
            CurrentValue = MaximumValue;
        }
    }

    public override void OnDeath(EntityDeathOptions details)
    {
        if (details.Cancel)
        {
            CurrentValue = MaximumValue;
            return;
        }

        CurrentValue = MinimumValue;
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new EntityHealthTrait(entity);
    }

    public override void OnRead(CompoundTag tag)
    {
        CurrentValue = tag.Get<FloatTag>("current")?.Value ?? CurrentValue;
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("current", new FloatTag { Value = CurrentValue });
    }
}






