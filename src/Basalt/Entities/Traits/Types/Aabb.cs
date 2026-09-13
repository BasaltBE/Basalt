namespace Basalt.Core.Entities.Traits.Types;

using Basalt.BedrockProtocol.Types;

public readonly struct Aabb(Vec3 min, Vec3 max) {
    public Vec3 Min { get; } = min;
    public Vec3 Max { get; } = max;

    public bool Contains(Vec3 point) {
        return point.X >= Min.X && point.X <= Max.X &&
            point.Y >= Min.Y && point.Y <= Max.Y &&
            point.Z >= Min.Z && point.Z <= Max.Z;
    }
}
