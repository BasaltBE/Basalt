using Basalt.Core;
using Basalt.Entity.Traits.Types;
using Basalt.Events;
using Basalt.Item.Traits;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Entity.Traits.PlayerTraits;
using Basalt.Protocol.Nbt;

namespace Basalt.Entity.Traits.Attribute;

public sealed class EntityHealthTrait : EntityAttributeTrait
{
    public new static string Identifier => "health";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:health"];

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

        if (Entity.Dimension is not null)
        {
            ActorEventPacket packet = new()
            {
                ActorRuntimeId = Entity.RuntimeId,
                Event = ActorEvent.Hurt,
                Data = (int)(signal.Cause ?? ActorDamageCause.None)
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
