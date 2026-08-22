namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Traits;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using System.Text.Json;

public sealed class EntityAttackTrait : EntityTrait {
    public new static string Identifier => "attack";
    public new static readonly string[] Components = ["minecraft:attack"];

    private const ulong DefaultAttackInterval = 20;
    private float _damage;
    private ulong _nextAttack;

    public EntityAttackTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        if (Entity.Type.TryGetComponentProperties("minecraft:attack", out JsonElement properties) &&
            properties.TryGetProperty("damage", out JsonElement damage) &&
            damage.ValueKind == JsonValueKind.Number &&
            damage.TryGetSingle(out float value)) {
            _damage = value;
        }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null ||
            Entity.GetTrait<EntityTargetingTrait>()?.Target is not Entity target ||
            !target.IsAlive || target.Dimension != Entity.Dimension) {
            return;
        }

        if (!CanReach(target) || details.CurrentTick < _nextAttack || _damage <= 0f) {
            return;
        }

        _nextAttack = details.CurrentTick + DefaultAttackInterval;
        target.GetTrait<EntityHealthTrait>()?.ApplyDamage(_damage, Entity, ActorDamageCause.EntityAttack);
    }

    private bool CanReach(Entity target) {
        EntityCollisionTrait? sourceCollision = Entity.GetTrait<EntityCollisionTrait>();
        EntityCollisionTrait? targetCollision = target.GetTrait<EntityCollisionTrait>();
        float sourceWidth = sourceCollision?.Width ?? EntityCollisionTrait.DefaultWidth;
        float targetWidth = targetCollision?.Width ?? EntityCollisionTrait.DefaultWidth;
        float sourceHeight = sourceCollision?.Height ?? EntityCollisionTrait.DefaultHeight;
        float targetHeight = targetCollision?.Height ?? EntityCollisionTrait.DefaultHeight;
        Vec3 sourceFeet = Entity.IsPlayer() ? Entity.GetPosition() : Entity.Position;
        Vec3 targetFeet = target.IsPlayer() ? target.GetPosition() : target.Position;
        float horizontalX = targetFeet.X - sourceFeet.X;
        float horizontalZ = targetFeet.Z - sourceFeet.Z;
        float horizontalDistance = MathF.Sqrt(horizontalX * horizontalX + horizontalZ * horizontalZ);
        float sourceTop = sourceFeet.Y + sourceHeight;
        float targetTop = targetFeet.Y + targetHeight;
        float verticalGap = MathF.Max(sourceFeet.Y - targetTop, targetFeet.Y - sourceTop);

        return horizontalDistance <= 1.25f + (sourceWidth + targetWidth) * 0.5f &&
            verticalGap <= 0.5f;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityAttackTrait(entity);
    }
}
