namespace Basalt.Core.Entities.Traits.Types;

using Basalt.Protocol.Types;

public sealed class RideableSeat
{
  public int Index { get; }
  public Vec3f Position { get; set; }
  public float SeatRotation { get; set; }
  public bool LockRotation { get; set; }
  public bool Driver { get; set; }

  public RideableSeat(int index, Vec3f position, float seatRotation = 0f, bool lockRotation = false, bool driver = false)
  {
    Index = index;
    Position = position;
    SeatRotation = seatRotation;
    LockRotation = lockRotation;
    Driver = driver;
  }
}
