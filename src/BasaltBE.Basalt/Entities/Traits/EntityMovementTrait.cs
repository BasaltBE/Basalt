using System.Numerics;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class EntityMovementTrait(Entity entity) : EntityTrait(entity)
{
    internal const float PositionBroadcastThreshold = 0.01f;
    internal const float RotationBroadcastThreshold = 1;
    internal Vector3 BroadcastPosition;
    internal Vector2 BroadcastRotation;
    internal float BroadcastHeadRotation;
    internal bool BroadcastOnGround;
    public readonly Vec3 Motion = new();
    public bool Jumping;
    public bool HorizontalCollision;
    public bool VerticalCollision;
    public ulong? InputTick;

    public override void OnSpawn()
    {
        BroadcastPosition = new Vector3(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
        BroadcastRotation = new Vector2(Entity.Rotation.X, Entity.Rotation.Y);
        BroadcastHeadRotation = Entity.HeadRotation;
        BroadcastOnGround = VerticalCollision && Motion.Y <= 0;
    }
}
