namespace Basalt.Core.Player.Traits;

using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Entity = Basalt.Core.Entities.Entity;
using Basalt.Core.Entities;
using Basalt.BedrockProtocol.Enums;

public sealed class PlayerHungerTrait : EntityAttributeTrait {
    public new static string Identifier => "hunger";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    public override AttributeName Attribute => AttributeName.PlayerHunger;

    public float Saturation;
    public float Exhaustion;

    // Exhaustion costs for actions
    private const float SprintCost = 0.1f;
    private const float SwimCost = 0.01f;
    private const float JumpCost = 0.05f;
    private const float SprintJumpCost = 0.2f;

    // Exhaustion draining
    private const float DrainAt = 4f;
    private const float SatDrain = 1f;
    private const float HungerDrain = 1f;

    // Regen / starve
    private const ulong RegenTicks = 30UL;
    private const float RegenAt = 17f;
    private const float RegenExhaustion = 6f;
    private const float StarveDamage = 1f;
    private const float DefaultSat = 0f;

    public PlayerHungerTrait(Entity entity) : base(entity) { }

    public override void OnAdd() {
        EnsureAttribute(new Basalt.Core.Entities.Traits.Types.AttributeProperties(0, 20, 20, 20));
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (Entity is not Player player)
            return;

        if (!CanLoseHunger(player))
            return;

        EntityHealthTrait? health = player.GetTrait<EntityHealthTrait>();
        if (health is null)
            return;

        TryDrainExhaustion();
        TickHungerEffects(health, player, details.CurrentTick);
    }

    public override void OnMove(EntityMoveOptions details) {
        if (Entity is not Player player || !CanLoseHunger(player))
            return;

        float deltaX = details.To.X - details.From.X;
        float deltaZ = details.To.Z - details.From.Z;

        if (player.IsSwimming) {
            float deltaY = details.To.Y - details.From.Y;
            Exhaustion += MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ) * SwimCost;
        }
        else if (player.IsSprinting) {
            Exhaustion += MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ) * SprintCost;
        }
    }

    public void OnJump() {
        if (!Entity.IsAlive)
            return;

        Exhaustion += Entity.IsSprinting
            ? JumpCost + SprintJumpCost
            : JumpCost;
    }

    public bool Eat(int nutrition, float saturationModifier, bool canAlwaysEat) {
        if (!canAlwaysEat && CurrentValue >= MaximumValue)
            return false;

        CurrentValue = Math.Clamp(CurrentValue + nutrition, MinimumValue, MaximumValue);
        Saturation = Math.Clamp(Saturation + nutrition * saturationModifier * 2f, 0f, CurrentValue);

        return true;
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (details.InitialSpawn)
            return;

        CurrentValue = DefaultValue;
        Saturation = DefaultSat;
        Exhaustion = 0f;
    }

    public override EntityTrait Clone(Entity entity) => new PlayerHungerTrait(entity);

    public override void OnRead(CompoundTag tag) {
        CurrentValue = tag.Get<FloatTag>("current")?.Value ?? CurrentValue;
        Saturation = tag.Get<FloatTag>("saturation")?.Value ?? Saturation;
        Exhaustion = tag.Get<FloatTag>("exhaustion")?.Value ?? Exhaustion;
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("current", new FloatTag { Value = CurrentValue });
        tag.Set("saturation", new FloatTag { Value = Saturation });
        tag.Set("exhaustion", new FloatTag { Value = Exhaustion });
    }

    private static bool CanLoseHunger(Player player) {
        var difficulty = player.Dimension?.Difficulty ?? Difficulty.Normal;
        if (difficulty == Difficulty.Peaceful)
            return false;

        if (!player.IsAlive || player.Abilities.GetAbility(PlayerAbility.Flying))
            return false;

        var gamemode = player.GetGamemode();
        return gamemode is not (GameType.Spectator or GameType.Creative);
    }

    private void TryDrainExhaustion() {
        if (Exhaustion < DrainAt)
            return;

        Exhaustion -= DrainAt;

        if (Saturation > 0f)
            Saturation = MathF.Max(0f, Saturation - SatDrain);
        else
            CurrentValue = MathF.Max(0f, CurrentValue - HungerDrain);
    }

    private void TickHungerEffects(EntityHealthTrait health, Player player, ulong tick) {
        if (tick % RegenTicks != 0UL)
            return;

        if (CurrentValue > RegenAt && health.CurrentValue < health.MaximumValue) {
            health.CurrentValue += 1f;
            Exhaustion += RegenExhaustion;
        }
        else if (CurrentValue <= 0f)
            health.ApplyDamage(StarveDamage, player, ActorDamageCause.Starve);
    }
}





