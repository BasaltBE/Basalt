namespace Basalt.Core.Entities.Traits.Types;

using Basalt.BedrockProtocol.Types;

public readonly struct Aabb(Vec3 min, Vec3 max) {
    public Vec3 Min { get; } = new() {
        X = MathF.Min(min.X, max.X), Y = MathF.Min(min.Y, max.Y), Z = MathF.Min(min.Z, max.Z)
    };
    public Vec3 Max { get; } = new() {
        X = MathF.Max(min.X, max.X), Y = MathF.Max(min.Y, max.Y), Z = MathF.Max(min.Z, max.Z)
    };

    public bool Contains(Vec3 point) {
        return point.X >= Min.X && point.X <= Max.X &&
            point.Y >= Min.Y && point.Y <= Max.Y &&
            point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public bool Intersects(in Aabb other) {
        return Min.X < other.Max.X && Max.X > other.Min.X &&
            Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
            Min.Z < other.Max.Z && Max.Z > other.Min.Z;
    }

    public Aabb Move(Vec3 offset) {
        return new(
            new Vec3 { X = Min.X + offset.X, Y = Min.Y + offset.Y, Z = Min.Z + offset.Z },
            new Vec3 { X = Max.X + offset.X, Y = Max.Y + offset.Y, Z = Max.Z + offset.Z });
    }

    public Aabb Inflate(float x, float y, float z) {
        return new(
            new Vec3 { X = Min.X - x, Y = Min.Y - y, Z = Min.Z - z },
            new Vec3 { X = Max.X + x, Y = Max.Y + y, Z = Max.Z + z });
    }

    public Aabb ExpandTowards(Vec3 delta) {
        return new(
            new Vec3 { X = delta.X < 0 ? Min.X + delta.X : Min.X, Y = delta.Y < 0 ? Min.Y + delta.Y : Min.Y, Z = delta.Z < 0 ? Min.Z + delta.Z : Min.Z },
            new Vec3 { X = delta.X > 0 ? Max.X + delta.X : Max.X, Y = delta.Y > 0 ? Max.Y + delta.Y : Max.Y, Z = delta.Z > 0 ? Max.Z + delta.Z : Max.Z });
    }
}
