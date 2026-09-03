using Basalt.BedrockProtocol.Types;

namespace Basalt.Core.Entities.Traits.Types;

public class MovementRotation {
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float HeadYaw { get; set; }
}


public readonly record struct EntityMoveOptions(
    Vec3 From, Vec3 To, MovementRotation FromRotation, MovementRotation ToRotation
);






