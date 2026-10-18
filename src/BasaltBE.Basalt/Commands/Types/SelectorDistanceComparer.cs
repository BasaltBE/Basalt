using System.Numerics;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands.Types;

internal sealed class SelectorDistanceComparer(Vector3 origin, bool farthest) : IComparer<Entity> {
    public int Compare(Entity? left, Entity? right) {
        if (left is null || right is null)
            return ReferenceEquals(left, right) ? 0 : left is null ? -1 : 1;
        float leftDistance = Vector3.DistanceSquared(new Vector3(left.Position.X, left.Position.Y, left.Position.Z), origin);
        float rightDistance = Vector3.DistanceSquared(new Vector3(right.Position.X, right.Position.Y, right.Position.Z), origin);
        int order = leftDistance.CompareTo(rightDistance);
        if (order == 0)
            order = left.RuntimeId.CompareTo(right.RuntimeId);
        return farthest ? -order : order;
    }
}
