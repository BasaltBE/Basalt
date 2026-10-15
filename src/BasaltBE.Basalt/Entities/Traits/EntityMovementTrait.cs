using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class EntityMovementTrait(Entity entity) : EntityTrait(entity) {
    public readonly Vec3 Motion = new();
    public bool Jumping;
    public bool HorizontalCollision;
    public bool VerticalCollision;
    public ulong? InputTick;
}
