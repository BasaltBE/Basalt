namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Core.Traits;


public sealed class EntityAirSupplyTrait : EntityTrait
{
    private const int MaxAirTicks = 300;

    public new static string Identifier => "air_supply";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private int _airTicks = MaxAirTicks;

    public EntityAirSupplyTrait(Entity entity) : base(entity)
    {
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        Entity.Flags.SetActorFlag(ActorFlag.Breathing, true);
        _airTicks = MaxAirTicks;
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Entity.IsAlive || !Entity.Flags.GetActorFlag(ActorFlag.Breathing))
        {
            return;
        }

        if (Entity is Player.Player player &&
            player.GetGamemode() is not (Gamemode.Survival or Gamemode.Adventure))
        {
            return;
        }

        if (CanBreathe())
        {
            if (_airTicks < MaxAirTicks)
            {
                _airTicks += 5;

                if (_airTicks > MaxAirTicks)
                {
                    _airTicks = MaxAirTicks;
                }
            }

            return;
        }

        _airTicks--;

        if (_airTicks > -20)
        {
            return;
        }

        _airTicks = 0;

        if (Entity.Dimension?.Gamerules.DrowningDamage == false)
        {
            return;
        }

        Entity.GetTrait<EntityHealthTrait>()?.ApplyDamage(
            0.5f,
            null,
            Entity.IsSwimming ? ActorDamageCause.Drowning : ActorDamageCause.Suffocation
        );
    }

    public int GetAirSupplyTicks()
    {
        return _airTicks;
    }

    public void SetAirSupplyTicks(int ticks)
    {
        _airTicks = ticks;
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new EntityAirSupplyTrait(entity);
    }

    private bool CanBreathe()
    {
        if (Entity.Dimension is null || Entity.HasEffect(EffectType.WaterBreathing))
        {
            return true;
        }

        Vec3f head = Entity.GetHeadLocation();

        string block = Entity.Dimension
            .GetPermutation(
                (int)MathF.Floor(head.X),
                (int)MathF.Floor(head.Y),
                (int)MathF.Floor(head.Z))
            .Type
            .Identifier;

        if (block.Contains("water", StringComparison.Ordinal) ||
            block.Contains("lava", StringComparison.Ordinal))
        {
            return false;
        }

        return block == "minecraft:air";
    }
}





