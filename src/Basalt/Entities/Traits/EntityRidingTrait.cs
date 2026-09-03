namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Basalt.BedrockProtocol.Types;

/// <summary>
/// Applied to an entity that is currently riding another entity.
/// </summary>
public sealed class EntityRidingTrait : EntityTrait {
    public new static string Identifier => "riding";

    /// <summary>
    /// The entity being ridden.
    /// </summary>
    public Entity Vehicle { get; }

    /// <summary>
    /// The seat this rider occupies.
    /// </summary>
    public RideableSeat Seat { get; }

    public EntityRidingTrait(Entity entity, Entity vehicle, RideableSeat seat) : base(entity) {
        Vehicle = vehicle;
        Seat = seat;
    }

    // Required for deserialization. Trait will be non-functional until properly re-linked.
    public EntityRidingTrait(Entity entity) : base(entity) {
        Vehicle = entity;
        Seat = new RideableSeat(0, new Vec3() { X = 0f, Y = 0f, Z = 0f }, 0f, false, false);
    }

    public Vec3 GetSeatPosition() {
        return new Vec3 {
            X = Seat.Position.X,
            Y = Seat.Position.Y,
            Z = Seat.Position.Z
        };
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Vehicle.Dimension is null || !Vehicle.IsAlive) {
            return;
        }

        UpdatePosition();
    }

    internal void UpdatePosition() {
        float yaw = Vehicle.Rotation.Y * (MathF.PI / 180f);
        float cos = MathF.Cos(yaw);
        float sin = MathF.Sin(yaw);
        Vec3 offset = GetSeatPosition();
        Vec3 vehiclePosition = Vehicle.Position;
        Vec3 position = new() {
            X = vehiclePosition.X + (offset.X * cos) - (offset.Z * sin),
            Y = vehiclePosition.Y + offset.Y,
            Z = vehiclePosition.Z + (offset.X * sin) + (offset.Z * cos)
        };

        Vec3 previousPosition = Entity.Position;
        if (previousPosition.X == position.X &&
            previousPosition.Y == position.Y &&
            previousPosition.Z == position.Z) {
            return;
        }

        Entity.Position = position;
        Entity.OnMove(new EntityMoveOptions(
            previousPosition,
            position,
            new MovementRotation {
                Pitch = Entity.Rotation.X,
                Yaw = Entity.Rotation.Y,
                HeadYaw = Entity.Rotation.Z
            },
            new MovementRotation {
                Pitch = Entity.Rotation.X,
                Yaw = Entity.Rotation.Y,
                HeadYaw = Entity.Rotation.Z
            }));

    }

    public override void OnRemove() {
        // Dismount handled by EntityRideableTrait.RemoveRider.
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityRidingTrait(entity, Vehicle, Seat);
    }
}
