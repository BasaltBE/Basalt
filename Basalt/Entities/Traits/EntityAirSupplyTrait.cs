namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Enums;
using Basalt.Core.Traits;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;

public sealed class EntityAirSupplyTrait : EntityTrait {
    private const int MaxAirTicks = 300;
    private const float LowPoseHeadOffset = 1.22f;

    public new static string Identifier => "air_supply";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private int _airTicks = MaxAirTicks;

    public EntityAirSupplyTrait(Entity entity) : base(entity) {
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        Entity.Flags.SetActorFlag(ActorFlag.Breathing, true);
        _airTicks = MaxAirTicks;
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || !Entity.Flags.GetActorFlag(ActorFlag.Breathing)) {
            return;
        }

        if (Entity is Player.Player player &&
            player.GetGamemode() is not (GameType.Survival or GameType.Adventure)) {
            return;
        }

        if (CanBreathe(out bool submerged)) {
            if (_airTicks < MaxAirTicks) {
                _airTicks += 5;

                if (_airTicks > MaxAirTicks) {
                    _airTicks = MaxAirTicks;
                }
            }

            return;
        }

        _airTicks--;

        if (_airTicks > -20) {
            return;
        }

        _airTicks = 0;

        if (Entity.Dimension?.Gamerules.DrowningDamage == false) {
            return;
        }

        Entity.GetTrait<EntityHealthTrait>()?.ApplyDamage(
            0.5f,
            null,
            submerged ? ActorDamageCause.Drowning : ActorDamageCause.Suffocation
        );
    }

    public int GetAirSupplyTicks() {
        return _airTicks;
    }

    public void SetAirSupplyTicks(int ticks) {
        _airTicks = ticks;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityAirSupplyTrait(entity);
    }

    private bool CanBreathe(out bool submerged) {
        submerged = false;
        if (Entity.Dimension is null || Entity.HasEffect(EffectType.WaterBreathing)) {
            return true;
        }

        Vec3 head = Entity.GetHeadLocation();
        if (Entity.IsSwimming || Entity.Flags.GetActorFlag(ActorFlag.Crawling)) {
            head.Y -= LowPoseHeadOffset;
        }

        BlockType block = Entity.Dimension
            .GetPermutation(
                (int)MathF.Floor(head.X),
                (int)MathF.Floor(head.Y),
                (int)MathF.Floor(head.Z))
            .Type;

        submerged = block.Liquid;
        return !submerged && !block.Solid;
    }
}





