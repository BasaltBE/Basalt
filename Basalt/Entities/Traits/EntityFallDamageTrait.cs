namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Item.Enchantment;
using Basalt.Core.Item.Traits;
using Basalt.Core.Player;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;

public sealed class EntityFallDamageTrait : EntityTrait {
    public new static string Identifier => "fall_damage";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:health"];

    private const float SafeDistance = 3.4f;
    private const float HayBaleReduction = 0.8f;
    private const int GraceTicks = 10;

    private float _fallDistance;
    private float _fallStartY;
    private bool _falling;
    private int _teleportGraceTicks;

    public EntityFallDamageTrait(Entity entity) : base(entity) { }

    public override void OnSpawn(EntitySpawnOptions details) {
        _fallDistance = 0f;
        _fallStartY = Entity.Position.Y;
        _falling = false;
    }

    public override void OnTeleport(EntityTeleportOptions details) {
        _fallDistance = 0f;
        _fallStartY = details.To.Y;
        _falling = false;
        _teleportGraceTicks = GraceTicks;
    }

    public override void OnMove(EntityMoveOptions details) {
        if (!Entity.IsPlayer()) return;

        if (_teleportGraceTicks > 0) {
            _teleportGraceTicks--;
            return;
        }

        float deltaY = details.To.Y - details.From.Y;
        bool wasGrounded = IsGrounded(details.From);
        bool isGrounded = IsGrounded(details.To);

        if (wasGrounded) {
            _fallStartY = details.From.Y;
            _falling = false;
        }

        if (deltaY > 0.001f) {
            return;
        }

        if (deltaY < -0.001f && !isGrounded) {
            if (!_falling) {
                _fallStartY = details.From.Y;
                _falling = true;
            }

            return;
        }

        if (!isGrounded || !_falling) return;

        _fallDistance = MathF.Max(0f, _fallStartY - details.To.Y);
        _falling = false;

        if (IsInLiquid(details.To)) {
            _fallDistance = 0f;
            return;
        }

        if (!isGrounded) return;

        ApplyFallDamage(details.To);
        _fallDistance = 0f;
    }

    public override void OnFallOnBlock(EntityFallOnBlockTraitEvent @event) {
        _fallDistance = @event.Distance;
        ApplyFallDamage(@event.Position);
        _fallDistance = 0f;
    }

    public override EntityTrait Clone(Entity entity) => new EntityFallDamageTrait(entity);

    private void ApplyFallDamage(Vec3 landingPosition) {
        if (!Entity.IsAlive) return;

        if (Entity is Player player) {
            if (player.GetGamemode() is GameType.Creative or GameType.Spectator) return;
            if (player.Abilities.GetAbility(PlayerAbility.Flying)) return;
        }

        if (Entity.Dimension?.Gamerules.FallDamage == false) return;

        float effectiveDistance = _fallDistance;
        if (effectiveDistance <= SafeDistance) return;

        int blockX = (int)MathF.Floor(landingPosition.X);
        int blockY = (int)MathF.Floor(landingPosition.Y) - 1;
        int blockZ = (int)MathF.Floor(landingPosition.Z);

        if (Entity.Dimension is not null) {
            string landedBlock = Entity.Dimension.GetLoadedPermutationOrAir(blockX, blockY, blockZ).Type.Identifier;
            float blockModifier = GetBlockDamageModifier(landedBlock);

            if (blockModifier <= 0f) return;
            effectiveDistance *= blockModifier;
        }

        if (effectiveDistance <= SafeDistance) return;

        float rawDamage = effectiveDistance - SafeDistance;

        float reduction = GetEnchantmentReduction(rawDamage);
        float finalDamage = rawDamage * (1f - Math.Clamp(reduction, 0f, 1f));

        if (finalDamage <= 0f) return;

        EntityHealthTrait? health = Entity.GetTrait<EntityHealthTrait>();
        health?.ApplyDamage(finalDamage, null, ActorDamageCause.Fall);
    }

    private bool IsGrounded(Vec3 position) {
        if (Entity.Dimension is null) return false;

        float halfWidth = Entity.GetTrait<EntityCollisionTrait>()?.Width * 0.5f
          ?? EntityCollisionTrait.DefaultWidth * 0.5f;
        int minX = (int)MathF.Floor(position.X - halfWidth + 0.001f);
        int maxX = (int)MathF.Floor(position.X + halfWidth - 0.001f);
        int blockY = (int)MathF.Floor(position.Y - 0.001f);
        int minZ = (int)MathF.Floor(position.Z - halfWidth + 0.001f);
        int maxZ = (int)MathF.Floor(position.Z + halfWidth - 0.001f);

        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                var block = Entity.Dimension.GetLoadedPermutationOrAir(blockX, blockY, blockZ).Type;
                if (block.Solid && !block.Air && !block.Liquid) return true;
            }
        }

        return false;
    }

    private bool IsInLiquid(Vec3 position) {
        if (Entity.Dimension is null) return false;

        int blockX = (int)MathF.Floor(position.X);
        int blockY = (int)MathF.Floor(position.Y);
        int blockZ = (int)MathF.Floor(position.Z);
        return Entity.Dimension.GetLoadedPermutationOrAir(blockX, blockY, blockZ).Type.Liquid;
    }

    private static float GetBlockDamageModifier(string blockIdentifier) {
        if (string.Equals(blockIdentifier, BlockIdentifier.Slime.ToIdentifier(), StringComparison.Ordinal))
            return 0f;

        if (string.Equals(blockIdentifier, BlockIdentifier.HayBlock.ToIdentifier(), StringComparison.Ordinal))
            return 1f - HayBaleReduction;

        if (string.Equals(blockIdentifier, BlockIdentifier.PowderSnow.ToIdentifier(), StringComparison.Ordinal))
            return 0f;

        if (blockIdentifier.Contains("water", StringComparison.Ordinal))
            return 0f;

        if (blockIdentifier.Contains("bed", StringComparison.Ordinal))
            return 0.5f;

        return 1f;
    }

    private float GetEnchantmentReduction(float rawDamage) {
        EntityEquipmentTrait? equipment = Entity.GetTrait<EntityEquipmentTrait>();
        if (equipment is null || Entity is not Player player) return 0f;

        HurtEnchantmentContext ctx = new() {
            Player = player,
            Damage = rawDamage,
            Source = DamageSource.Fall
        };

        for (int i = 0; i < equipment.Armor.GetSize(); i++) {
            ItemStack? item = equipment.Armor.GetItem(i);
            if (item is null) continue;

            ItemStackEnchantmentTrait? enchantments = item.GetTrait<ItemStackEnchantmentTrait>();
            enchantments?.OnHurt(ctx);
        }

        return ctx.DamageReduction;
    }
}
