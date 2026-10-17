using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Types.Components;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Basalt.Worlds;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class EntityRuntimeTests
{
    [Fact]
    public void EntityTypeComponentsInitializeHealthMovementAndBounds()
    {
        EntityType pigType = EntityPalette.Find("minecraft:pig")!;
        Entity pig = new(pigType);
        EntityHealthTrait health = new(pig);
        EntityMovementTrait movement = new(pig);
        pig.AddTrait(health);
        pig.AddTrait(movement);

        Assert.Equal(10, health.Current);
        Assert.Equal(10, health.Maximum);
        Assert.Equal(0.25f, movement.MovementSpeed);
    }

    [Fact]
    public void HealthRangeUsesItsMaximumAsCurrentHealth()
    {
        EntityType type = new()
        {
            Identifier = "minecraft:range_health_test",
            PropertiesPayload = new EntityTypePayload
            {
                Components = new EntityComponentPayloads
                {
                    Health = new EntityHealthComponent
                    {
                        Value = new EntityNumericValue
                        {
                            Range = new EntityNumericRange { RangeMin = 15, RangeMax = 30 }
                        }
                    }
                }
            }
        };
        Entity entity = new(type);
        EntityHealthTrait health = new(entity);
        entity.AddTrait(health);

        Assert.Equal(30, health.Maximum);
        Assert.Equal(30, health.Current);
    }

    [Fact]
    public void DamageEmitsCauseAndDeathRemovesEntity()
    {
        using MultiplayerFixture fixture = new();
        Entity pig = new(EntityPalette.Find("minecraft:pig"));
        EntityHealthTrait health = new(pig);
        HurtRecorder recorder = new(pig);
        pig.AddTrait(health);
        pig.AddTrait(recorder);
        fixture.Dimension.AddEntity(pig);

        EntityDamageCause? hurtCause = null;
        EntityDamageCause? deathCause = null;
        fixture.Server.Events.Subscribe<EntityHurtEvent>(hurt => hurtCause = hurt.Cause);
        fixture.Server.Events.Subscribe<EntityDeathEvent>(death => deathCause = death.Cause);

        Assert.True(health.ApplyDamage(4, EntityDamageCause.Lava));
        Assert.Equal(6, health.Current);
        Assert.Equal(EntityDamageCause.Lava, hurtCause);
        Assert.Equal(EntityDamageCause.Lava, recorder.Hurt!.Cause);
        Assert.True(health.ApplyDamage(6, EntityDamageCause.Void));
        Assert.True(pig.Dead);
        Assert.Equal(EntityDamageCause.Void, deathCause);
        Assert.Null(fixture.Dimension.GetEntity(pig.RuntimeId));
    }

    [Fact]
    public void CanceledHurtLeavesHealthUnchanged()
    {
        using MultiplayerFixture fixture = new();
        Entity pig = new(EntityPalette.Find("minecraft:pig"));
        EntityHealthTrait health = new(pig);
        pig.AddTrait(health);
        fixture.Dimension.AddEntity(pig);
        fixture.Server.Events.Subscribe<EntityHurtEvent>(hurt => hurt.Cancel());

        Assert.False(health.ApplyDamage(2, EntityDamageCause.Entity));
        Assert.Equal(10, health.Current);
    }

    [Fact]
    public void CanceledDeathPreventsLethalDamage()
    {
        using MultiplayerFixture fixture = new();
        Entity pig = new(EntityPalette.Find("minecraft:pig"));
        EntityHealthTrait health = new(pig);
        pig.AddTrait(health);
        fixture.Dimension.AddEntity(pig);
        fixture.Server.Events.Subscribe<EntityDeathEvent>(death => death.Cancel());

        Assert.False(health.ApplyDamage(10, EntityDamageCause.Void));
        Assert.Equal(10, health.Current);
        Assert.False(pig.Dead);
        Assert.Same(pig, fixture.Dimension.GetEntity(pig.RuntimeId));
    }

    [Fact]
    public void MovementTraitAppliesGravityAndStopsAtSolidBlocks()
    {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(0, 0, 0,
            BasaltBE.Basalt.Blocks.BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        Entity pig = new(EntityPalette.Find("minecraft:pig"));
        pig.Position.X = 0.5f;
        pig.Position.Y = 4;
        pig.Position.Z = 0.5f;
        pig.AddTrait(new EntityMovementTrait(pig));
        pig.Spawn(fixture.Dimension);

        for (int tick = 0; tick < 100; tick++)
            pig.Tick();

        Assert.Equal(1, pig.Position.Y);
        Assert.Equal(0, pig.GetTrait<EntityMovementTrait>()!.Motion.Y);
        Assert.True(pig.Flags.Get(ActorFlag.HasGravity));
    }

    private sealed class HurtRecorder(Entity entity) : EntityTrait(entity)
    {
        public EntityHurtEvent? Hurt;

        public override void OnHurt(EntityHurtEvent hurt) => Hurt = hurt;
    }
}
