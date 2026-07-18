namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Types;
using Basalt.Protocol.Types;

/// <summary>
/// Applied to an entity that is currently riding another entity.
/// </summary>
public sealed class EntityRidingTrait : EntityTrait
{
  public new static string Identifier => "riding";

  /// <summary>
  /// The entity being ridden.
  /// </summary>
  public Entity Vehicle { get; }

  /// <summary>
  /// The seat this rider occupies.
  /// </summary>
  public RideableSeat Seat { get; }

  public EntityRidingTrait(Entity entity, Entity vehicle, RideableSeat seat) : base(entity)
  {
    Vehicle = vehicle;
    Seat = seat;
  }

  // Required for deserialization. Trait will be non-functional until properly re-linked.
  public EntityRidingTrait(Entity entity) : base(entity)
  {
    Vehicle = entity;
    Seat = new RideableSeat(0, new Vec3f(0f, 0f, 0f), 0f, false, false);
  }

  public Vec3f GetSeatPosition()
  {
    return Seat.Position;
  }

  public override void OnRemove()
  {
    // Dismount handled by EntityRideableTrait.RemoveRider.
  }

  public override EntityTrait Clone(Entity entity)
  {
    return new EntityRidingTrait(entity, Vehicle, Seat);
  }
}
