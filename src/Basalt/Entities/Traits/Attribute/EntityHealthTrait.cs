namespace Basalt.Core.Entities.Traits.Attribute;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Events;
using Basalt.Core.Item.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Enums;
using Basalt.Core.Player.Traits;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Blocks;
using Basalt.Core.Worlds;
using System.Text.Json;

using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.NBT;

public sealed class EntityHealthTrait : EntityAttributeTrait {
    public new static string Identifier => "health";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:health"];
    private const float KnockbackHorizontalForce = 0.28f;
    private const float KnockbackVerticalForce = 0.38f;
    private const float KnockbackVerticalLimit = 0.4f;
    private const ulong KnockbackCooldownTicks = 10;
    private const ulong AttackCooldownTicks = 10;
    private const int DaylightFireTicks = 160;
    private ulong? _lastKnockbackTick;
    private ulong? _lastAttackTick;

    public ActorDamageCause? LastDamageCause { get; private set; }

    public override AttributeName Attribute => AttributeName.Health;

    public EntityHealthTrait(Entity entity) : base(entity) {
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || !BurnsInDaylight(Entity) ||
            Entity.Dimension is not Dimension dimension || dimension.Type != DimensionId.Overworld ||
            dimension.World is not Tickable world) {
            return;
        }

        float height = Entity.GetTrait<EntityCollisionTrait>()?.Height ?? 1.8f;
        Vec3 eyePosition = new() {
            X = Entity.Position.X,
            Y = Entity.Position.Y + height * 0.85f,
            Z = Entity.Position.Z
        };
        if (!dimension.IsDay() || !dimension.CanSeeSky(eyePosition)) {
            return;
        }

        Entity.SetOnFire(DaylightFireTicks);
    }

    private static bool BurnsInDaylight(Entity entity) {
        // TODO: use EntityIdentifier.Skeleton.ToIdentifierString()
        return entity.Type.Components.Contains("minecraft:burns_in_daylight") ||
            entity.Identifier is "minecraft:skeleton" or "minecraft:stray" or "minecraft:bogged" or
                "minecraft:wither_skeleton" or "minecraft:zombie" or "minecraft:husk" or
                "minecraft:zombie_villager" or "minecraft:zombie_villager_v2" or
                "minecraft:zombified_piglin";
    }

    public void ApplyDamage(float amount, Entity? damager = null, ActorDamageCause? cause = null) {
        EntityHurtSignal signal = new(Entity, amount, cause, damager);
        Entity.Dimension?.World?.Server?.Emit(signal);
        if (!signal.Emit()) {
            return;
        }

        if (Entity.HasEffect(EffectType.FireResistance) &&
            signal.Cause is ActorDamageCause.Fire or ActorDamageCause.FireTick or
            ActorDamageCause.Lava or ActorDamageCause.Magma or
            ActorDamageCause.Campfire or ActorDamageCause.SoulCampfire) {
            return;
        }

        if (Entity.HasEffect(EffectType.Resistance) && signal.Amount > 0f) {
            signal.Amount *= 0.8f;
        }

        LastDamageCause = signal.Cause;
        Entity.OnHurt(new EntityHurtDetails(signal.Cause, signal.Damager));

        if (signal.Cause == ActorDamageCause.EntityAttack && signal.Amount > 0f && Entity.Dimension?.World is Tickable cooldownTickable) {
            ulong currentTick = cooldownTickable.TickValue;
            if (_lastAttackTick is ulong lastAttackTick &&
                currentTick >= lastAttackTick &&
                currentTick - lastAttackTick < AttackCooldownTicks) {
                return;
            }

            _lastAttackTick = currentTick;
        }

        CurrentValue -= signal.Amount;
        bool knockbackApplied = false;
        ulong knockbackTick = 0;
        if (signal.Cause == ActorDamageCause.EntityAttack && damager is not null && Entity.Dimension is not null && damager.Dimension == Entity.Dimension) {
            float knockbackResistance = Math.Clamp(
                Entity.Attributes.GetAttribute(AttributeName.KnockbackResistance)?.Current ?? 0f,
                0f,
                1f);
            ulong currentTick = Entity.Dimension.World is Tickable tickable ? tickable.TickValue : 0;
            if (knockbackResistance < 1f &&
                (_lastKnockbackTick is null || currentTick - _lastKnockbackTick.Value >= KnockbackCooldownTicks)) {
                float x = Entity.Position.X - damager.Position.X;
                float z = Entity.Position.Z - damager.Position.Z;
                float length = MathF.Sqrt((x * x) + (z * z));
                if (length > 0.0001f) {
                    float invLength = 1f / length;
                    float velocityX = Entity.Velocity.X * 0.5f;
                    float velocityY = Entity.Velocity.Y * 0.5f;
                    float velocityZ = Entity.Velocity.Z * 0.5f;
                    float multiplier = 1f - knockbackResistance;
                    velocityX += x * invLength * KnockbackHorizontalForce * multiplier;
                    velocityY += KnockbackVerticalForce * multiplier;
                    velocityZ += z * invLength * KnockbackHorizontalForce * multiplier;
                    if (velocityY > KnockbackVerticalLimit) {
                        velocityY = KnockbackVerticalLimit;
                    }

                    Vec3 knockbackVelocity = new() {
                        X = velocityX,
                        Y = velocityY,
                        Z = velocityZ
                    };
                    Entity.GetTrait<EntityMovementTrait>()?.ApplyExternalImpulse(knockbackVelocity, KnockbackCooldownTicks);
                    if (Entity.GetTrait<EntityMovementTrait>() is null) {
                        Entity.Velocity = knockbackVelocity;
                    }
                    if (Entity.Identifier is "minecraft:villager" or "minecraft:villager_v2") {
                        Logger.Debug("Villager knockback: damage={0:0.###} from=({1:0.###},{2:0.###},{3:0.###}) velocity=({4:0.###},{5:0.###},{6:0.###})", signal.Amount, damager.Position.X, damager.Position.Y, damager.Position.Z, knockbackVelocity.X, knockbackVelocity.Y, knockbackVelocity.Z);
                    }
                    _lastKnockbackTick = currentTick;
                    knockbackApplied = true;
                    knockbackTick = currentTick;
                }
            }
        }
        if (Entity.Dimension is not null) {
            ActorEventPacket packet = new() {
                ActorRuntimeId = Entity.RuntimeId,
                EventId = 2,
                Data = (int)(signal.Cause ?? ActorDamageCause.Fall)
            };
            Entity.Dimension.Broadcast(packet);

            if (knockbackApplied) {
                Entity.Dimension.Broadcast(new SetActorMotionPacket {
                    Motion = Entity.Velocity,
                    ActorRuntimeId = Entity.RuntimeId
                    ,
                    Tick = knockbackTick
                });
            }
        }

        EntityEquipmentTrait? equipment = Entity.GetTrait<EntityEquipmentTrait>();
        if (equipment is not null) {
            for (int i = 0; i < equipment.Armor.GetSize(); i++) {
                if (equipment.Armor.GetItem(i) is not { } itemStack) {
                    continue;
                }

                ItemStackDurabilityTrait? durability = itemStack.GetTrait<ItemStackDurabilityTrait>();
                durability?.ApplyArmorDamage(equipment.Armor, i);
            }
        }

        PlayerHungerTrait? hungerTrait = Entity.GetTrait<PlayerHungerTrait>();
        if (hungerTrait is not null) {
            hungerTrait.Exhaustion += 0.1f;
        }

        if (CurrentValue <= 0) {
            if (Entity is Player.Player player) {
                Entity.OnDeath(new EntityDeathOptions(KillerSource: damager, DamageCause: signal.Cause));

                player.Send(new RespawnPacket {
                    Position = player.Dimension?.SpawnPosition ?? player.Location,
                    State = PlayerRespawnState.SearchingForSpawn,
                    PlayerRuntimeId = player.RuntimeId
                });
            }
            else {
                Entity.Kill(new EntityDeathOptions(KillerSource: damager, DamageCause: signal.Cause));
            }
        }
    }

    public void Heal(float amount) {
        if (amount <= 0f || !Entity.IsAlive) {
            return;
        }

        CurrentValue = MathF.Min(MaximumValue, CurrentValue + amount);
    }

    public override void OnAdd() {
        AttributeProperties properties = GetHealthProperties();
        if (Entity.Attributes.GetAttribute(Attribute) is AttributeData attribute) {
            attribute.Minimum = properties.MinimumValue ?? attribute.Minimum;
            attribute.Maximum = properties.MaximumValue ?? attribute.Maximum;
            attribute.DefaultMinimum = properties.MinimumValue ?? attribute.DefaultMinimum;
            attribute.DefaultMaximum = properties.MaximumValue ?? attribute.DefaultMaximum;
            attribute.Default = properties.DefaultValue ?? attribute.Default;
            attribute.Current = properties.CurrentValue ?? attribute.Current;
            Entity.Attributes.SetAttribute(attribute);
            Entity.AttributesDirty = true;
            return;
        }

        EnsureAttribute(properties);
    }

    private AttributeProperties GetHealthProperties() {
        const float DefaultHealth = 20f;
        if (!Entity.Type.TryGetComponentProperties("minecraft:health", out JsonElement health)) {
            return new AttributeProperties(0, DefaultHealth, DefaultHealth, DefaultHealth);
        }

        float max = ReadFloat(health, "max") ?? DefaultHealth;
        float current = ReadFloat(health, "value") ?? max;
        return new AttributeProperties(0, max, max, current);
    }

    private static float? ReadFloat(JsonElement element, string property) {
        if (!element.TryGetProperty(property, out JsonElement value) || value.ValueKind != JsonValueKind.Number) {
            return null;
        }

        return value.TryGetSingle(out float result) ? result : null;
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (details.InitialSpawn) {
            if (CurrentValue <= MinimumValue) {
                CurrentValue = DefaultValue;
            }
            return;
        }

        CurrentValue = DefaultValue;
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        if (details.Disconnected && CurrentValue <= MinimumValue) {
            CurrentValue = MaximumValue;
        }
    }

    public override void OnDeath(EntityDeathOptions details) {
        if (details.Cancel) {
            CurrentValue = MaximumValue;
            return;
        }

        CurrentValue = MinimumValue;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityHealthTrait(entity);
    }

    public override void OnRead(CompoundTag tag) {
        CurrentValue = tag.Get<FloatTag>("current")?.Value ?? CurrentValue;
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("current", new FloatTag { Value = CurrentValue });
    }
}






