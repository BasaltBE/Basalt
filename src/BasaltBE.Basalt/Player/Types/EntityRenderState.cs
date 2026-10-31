using BasaltBE.Basalt.Entities.Traits;

namespace BasaltBE.Basalt.Entities.Types;

internal sealed class EntityRenderState(Entity entity) {
    internal readonly Entity Entity = entity;
    internal ulong Seen;
    internal float X = entity.Position.X;
    internal float Y = entity.Position.Y;
    internal float Z = entity.Position.Z;
    internal float Pitch = entity.Rotation.X;
    internal float Yaw = entity.Rotation.Y;
    internal float HeadYaw = entity.HeadRotation;
    internal bool Grounded = entity.GetTrait<EntityMovementTrait>()?.Grounded ?? false;
    internal int TicksSinceMovementUpdate;
    internal float MotionX = entity.GetTrait<EntityMovementTrait>()?.Motion.X ?? 0;
    internal float MotionY = entity.GetTrait<EntityMovementTrait>()?.Motion.Y ?? 0;
    internal float MotionZ = entity.GetTrait<EntityMovementTrait>()?.Motion.Z ?? 0;
}
