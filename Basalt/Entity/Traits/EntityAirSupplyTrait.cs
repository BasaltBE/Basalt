using Basalt.Entity.Traits.Attribute;
using Basalt.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Traits;

namespace Basalt.Entity.Traits;

public sealed class EntityAirSupplyTrait : EntityTrait
{
    public new static string Identifier => "air_supply";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    private int _airSupplyTicks = 300;

    public EntityAirSupplyTrait(Entity entity) : base(entity)
    {
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Entity.IsAlive || !Entity.Flags.GetActorFlag(ActorFlag.Breathing))
        {
            return;
        }

        if (Entity is Core.Player player)
        {
            Gamemode gamemode = player.GetGamemode();
            if (gamemode is not (Gamemode.Survival or Gamemode.Adventure))
            {
                return;
            }
        }

        int currentAirTicks = GetAirSupplyTicks();
        bool canBreathe = CanBreathe();

        if (!canBreathe)
        {
            SetAirSupplyTicks(currentAirTicks - 1);
            if (currentAirTicks > -20)
            {
                return;
            }

            SetAirSupplyTicks(0);

            EntityHealthTrait? health = Entity.GetTrait<EntityHealthTrait>();
            if (health is null)
            {
                return;
            }

            bool drowningDamage = Entity.Dimension?.Gamerules.DrowningDamage ?? true;
            if (!drowningDamage)
            {
                return;
            }

            health.ApplyDamage(
                0.5f,
                null,
                Entity.IsSwimming ? ActorDamageCause.Drowning : ActorDamageCause.Suffocation
            );

            return;
        }

        if (currentAirTicks >= 300)
        {
            return;
        }

        SetAirSupplyTicks(currentAirTicks + 5);
    }

    private bool CanBreathe()
    {
        if (Entity.Dimension is null)
        {
            return true;
        }

        var head = Entity.GetHeadLocation();
        int x = (int)MathF.Floor(head.X);
        int y = (int)MathF.Floor(head.Y);
        int z = (int)MathF.Floor(head.Z);
        string identifier = Entity.Dimension.GetPermutation(x, y, z).Type.Identifier;
        bool isLiquid = identifier.Contains("water", StringComparison.Ordinal) ||
                        identifier.Contains("lava", StringComparison.Ordinal);
        bool isSolid = !isLiquid && !string.Equals(identifier, "minecraft:air", StringComparison.Ordinal);

        return (!isLiquid && !isSolid) || Entity.HasEffect(EffectType.WaterBreathing);
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (!Entity.Flags.GetActorFlag(ActorFlag.Breathing))
        {
            Entity.Flags.SetActorFlag(ActorFlag.Breathing, true);
        }
        _airSupplyTicks = 300;
    }

    public int GetAirSupplyTicks()
    {
        return _airSupplyTicks;
    }

    public void SetAirSupplyTicks(int ticks)
    {
        _airSupplyTicks = ticks;
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new EntityAirSupplyTrait(entity);
    }
}
