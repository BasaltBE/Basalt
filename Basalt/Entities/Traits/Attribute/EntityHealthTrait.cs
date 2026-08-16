namespace Basalt.Core.Entities.Traits.Attribute;

using Basalt.Core.Events;
using Basalt.Core.Item.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player.Traits;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Blocks;
using Basalt.Core.Worlds;
using System.Text.Json;

using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;
using BedrockProtocol.Nbt;

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
        if (!Entity.IsAlive || !Entity.Type.Components.Contains("minecraft:burns_in_daylight") ||
            Entity.Dimension is not Dimension dimension || dimension.Type != DimensionId.Overworld ||
            dimension.World is not Tickable world) {
            return;
        }

        if (!dimension.IsDay() || !CanSeeSky(dimension)) {
            return;
        }

        Entity.SetOnFire(DaylightFireTicks);
    }

    public void ApplyDamage(float amount, Entity? damager = null, ActorDamageCause? cause = null) {
        EntityHurtSignal signal = new(Entity, amount, cause, damager);
        Entity.Dimension?.World?.Server?.Emit(signal);
        if (!signal.Emit()) {
            return;
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
            ulong currentTick = Entity.Dimension.World is Tickable tickable ? tickable.TickValue : 0;
            if (_lastKnockbackTick is null || currentTick - _lastKnockbackTick.Value >= KnockbackCooldownTicks) {
                float x = Entity.Position.X - damager.Position.X;
                float z = Entity.Position.Z - damager.Position.Z;
                float length = MathF.Sqrt((x * x) + (z * z));
                if (length > 0.0001f) {
                    float invLength = 1f / length;
                    float velocityX = Entity.Velocity.X * 0.5f;
                    float velocityY = Entity.Velocity.Y * 0.5f;
                    float velocityZ = Entity.Velocity.Z * 0.5f;
                    velocityX += x * invLength * KnockbackHorizontalForce;
                    velocityY += KnockbackVerticalForce;
                    velocityZ += z * invLength * KnockbackHorizontalForce;
                    if (velocityY > KnockbackVerticalLimit) {
                        velocityY = KnockbackVerticalLimit;
                    }

                    Entity.Velocity = new Vec3 {
                        X = velocityX,
                        Y = velocityY,
                        Z = velocityZ
                    };
                    _lastKnockbackTick = currentTick;
                    knockbackApplied = true;
                    knockbackTick = currentTick;
                }
            }
        }
        if (Entity.Dimension is not null) {
            ActorEventPacket packet = new() {
                TargetRuntimeID = new ActorRuntimeID() {
                    Value = Entity.RuntimeId,
                },
                EventID = ActorEvent.HURT,
                Data = (int)(signal.Cause ?? ActorDamageCause.Fall)
            };
            Entity.Dimension.Broadcast(packet);

            if (knockbackApplied) {
                Entity.Dimension.Broadcast(new SetActorMotionPacket {
                    Motion = Entity.Velocity,
                    TargetRuntimeID = new ActorRuntimeID {
                        Value = Entity.RuntimeId
                    },
                    Tick = new PlayerInputTick {
                        InputTick = knockbackTick
                    }
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
                    PlayerRuntimeId = new() {
                        Value = player.RuntimeId,
                    }
                });
            }
            else {
                Entity.Kill(new EntityDeathOptions(KillerSource: damager, DamageCause: signal.Cause));
            }
        }
    }

    public override void OnAdd() {
        AttributeProperties properties = GetHealthProperties();
        if (Entity.Attributes.GetAttribute(Attribute) is AttributeData attribute) {
            attribute.MinValue = properties.MinimumValue ?? attribute.MinValue;
            attribute.MaxValue = properties.MaximumValue ?? attribute.MaxValue;
            attribute.DefaultMinValue = properties.MinimumValue ?? attribute.DefaultMinValue;
            attribute.DefaultMaxValue = properties.MaximumValue ?? attribute.DefaultMaxValue;
            attribute.DefaultValue = properties.DefaultValue ?? attribute.DefaultValue;
            attribute.CurrentValue = properties.CurrentValue ?? attribute.CurrentValue;
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

    private bool CanSeeSky(Dimension dimension) {
        int x = (int)MathF.Floor(Entity.Position.X);
        int y = (int)MathF.Floor(Entity.Position.Y + 0.1f);
        int z = (int)MathF.Floor(Entity.Position.Z);

        for (int currentY = y; currentY <= World.MaxY; currentY++) {
            if (!dimension.TryGetLoadedPermutation(x, currentY, z, out BlockPermutation? permutation)) {
                return true;
            }

            if (!permutation!.Type.Air && !permutation.Type.Liquid && permutation.Type.Opacity >= 1f) {
                return false;
            }
        }

        return true;
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






