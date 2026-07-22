namespace Basalt.Core.Entities.Traits;

using Basalt.Protocol.Enums;
using System.Text.Json;

public sealed class EntityCollisionTrait : EntityTrait {
    public new static string Identifier => "collision";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Item];
    public new static readonly string[] Components = ["minecraft:collision_box"];

    public const float DefaultHeight = 1.62f;
    public const float DefaultWidth = 0.6f;

    public float Height = DefaultHeight;
    public float Width = DefaultWidth;
    public float FrictionForce = 1f;
    public float FrictionScaler = 0.97f;
    public int XAxisCollision;
    public int YAxisCollision;
    public int ZAxisCollision;

    public EntityCollisionTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        if (Entity.Identifier == "minecraft:item") {
            Height = 0.25f;
            Width = 0.25f;
        }
        else if (Entity.Type.TryGetComponentProperties("minecraft:collision_box", out JsonElement collisionBox)) {
            Height = ReadFloat(collisionBox, "height") ?? DefaultHeight;
            Width = ReadFloat(collisionBox, "width") ?? DefaultWidth;
        }

        if (!Entity.Flags.GetActorFlag(ActorFlag.HasCollision)) {
            Entity.Flags.SetActorFlag(ActorFlag.HasCollision, true);
        }
    }

    public override void OnRemove() {
        Entity.Flags.SetActorFlag(ActorFlag.HasCollision, false);
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityCollisionTrait(entity) {
            Height = Height,
            Width = Width,
            FrictionForce = FrictionForce,
            FrictionScaler = FrictionScaler,
            XAxisCollision = XAxisCollision,
            YAxisCollision = YAxisCollision,
            ZAxisCollision = ZAxisCollision
        };
    }

    private static float? ReadFloat(JsonElement element, string property) {
        if (!element.TryGetProperty(property, out JsonElement value) || value.ValueKind != JsonValueKind.Number) {
            return null;
        }

        return value.TryGetSingle(out float result) ? result : null;
    }
}
