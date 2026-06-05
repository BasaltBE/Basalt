namespace Basalt.Core.Entity.Traits.Types;

using Basalt.Protocol.Types;

public class MovementRotation {
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float HeadYaw { get; set; }
}


public readonly record struct EntityMoveOptions(
    Vec3f From, Vec3f To, MovementRotation FromRotation, MovementRotation ToRotation
);






