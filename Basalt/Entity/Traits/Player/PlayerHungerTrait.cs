using Basalt.Core;
using Basalt.Entity.Traits.Attribute;
using Basalt.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Traits;

namespace Basalt.Entity.Traits.PlayerTraits;

public sealed class PlayerHungerTrait : EntityAttributeTrait
{
    public new static string Identifier => "hunger";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    public override AttributeName Attribute => AttributeName.PlayerHunger;

    public float Saturation  = 10f;
    public float Exhaustion;

    public PlayerHungerTrait(Entity entity) : base(entity)
    {
    }

    public override void OnAdd()
    {
        EnsureAttribute(new AttributeProperties(0, 20, 20, 20));
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (Entity is not Core.Player player)
        {
            return;
        }

        Difficulty difficulty = player.Dimension?.Difficulty ?? Difficulty.Normal;
        if (difficulty == Difficulty.Peaceful)
        {
            return;
        }

        bool isFlying = player.Abilities.GetAbility(AbilityIndex.Flying);
        if (!player.IsAlive || isFlying)
        {
            return;
        }

        Gamemode gamemode = player.GetGamemode();
        if (gamemode is Gamemode.Spectator or Gamemode.Creative)
        {
            return;
        }

        EntityHealthTrait? health = player.GetTrait<EntityHealthTrait>();
        if (health is null)
        {
            return;
        }

        if (player.IsSprinting)
        {
            Exhaustion += 0.1f;
        }

        if (player.IsSwimming)
        {
            Exhaustion += 0.01f;
        }

        if (Exhaustion >= 4f)
        {
            Exhaustion -= 4f;
            if (Saturation > 0f)
            {
                Saturation = MathF.Max(0f, Saturation - 1f);
            }
            else if (CurrentValue > 0f)
            {
                CurrentValue -= 1f;
            }
        }

        ulong currentTick = details.CurrentTick;
        if (CurrentValue > 17f && currentTick % 30UL == 0UL)
        {
            if (health.CurrentValue < 20f)
            {
                health.CurrentValue += 1f;
            }
        }
        else if (CurrentValue <= 0f && currentTick % 30UL == 0UL)
        {
            health.ApplyDamage(1f, player, ActorDamageCause.Starve);
        }
    }

    public void OnJump()
    {
        if (!Entity.IsAlive)
        {
            return;
        }

        Exhaustion += 0.05f;
        if (Entity.IsSprinting)
        {
            Exhaustion += 0.2f;
        }
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (details.InitialSpawn)
        {
            return;
        }

        CurrentValue = DefaultValue;
        Saturation = 10f;
        Exhaustion = 0f;
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new PlayerHungerTrait(entity);
    }

    public override void OnRead(CompoundTag tag)
    {
        CurrentValue = tag.Get<FloatTag>("current")?.Value ?? CurrentValue;
        Saturation = tag.Get<FloatTag>("saturation")?.Value ?? Saturation;
        Exhaustion = tag.Get<FloatTag>("exhaustion")?.Value ?? Exhaustion;
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("current", new FloatTag { Value = CurrentValue });
        tag.Set("saturation", new FloatTag { Value = Saturation });
        tag.Set("exhaustion", new FloatTag { Value = Exhaustion });
    }
}
