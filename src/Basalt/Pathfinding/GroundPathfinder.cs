namespace Basalt.Core.Pathfinding;

using Basalt.Core.Profiling;

public static class GroundPathfinder {
    private const float DiagonalDistance = 1.4142135f;

    private static readonly (int X, int Z)[] Directions = [
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1),
        (1, 1),
        (1, -1),
        (-1, 1),
        (-1, -1)
    ];

    public static Path? FindPath(
        PathfindingSnapshot snapshot,
        PathNode start,
        PathNode target,
        int maxVisitedNodes = 4096,
        float maxDistance = 32f) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("GroundPathfinder.FindPath") : default;
        if (!snapshot.Walkable(start.X, start.Y, start.Z)) {
            return null;
        }

        PriorityQueue<PathNode, float> open = new();
        Dictionary<PathNode, float> scores = [];
        Dictionary<PathNode, PathNode> previous = [];
        HashSet<PathNode> closed = [];
        PathNode closest = start;
        float closestDistanceSquared = DistanceSquared(start, target);
        float maxDistanceSquared = maxDistance * maxDistance;
        int visited = 0;

        scores[start] = 0f;
        open.Enqueue(start, MathF.Sqrt(closestDistanceSquared));

        while (open.TryDequeue(out PathNode current, out _)) {
            visited++;
            if (visited > maxVisitedNodes) {
                break;
            }

            if (!closed.Add(current)) {
                continue;
            }

            float distanceSquared = DistanceSquared(current, target);
            if (distanceSquared < closestDistanceSquared) {
                closest = current;
                closestDistanceSquared = distanceSquared;
            }

            if (distanceSquared == 0f) {
                return CreatePath(snapshot, previous, current, target, true);
            }

            foreach ((int x, int z) in Directions) {
                TryVisit(current, new PathNode(current.X + x, current.Y, current.Z + z));
                if (x == 0 || z == 0) {
                    TryVisit(current, new PathNode(current.X + x, current.Y + 1, current.Z + z));
                    TryVisit(current, new PathNode(current.X + x, current.Y - 1, current.Z + z));
                }
            }

            void TryVisit(PathNode from, PathNode candidate) {
                int deltaX = candidate.X - from.X;
                int deltaZ = candidate.Z - from.Z;
                if (deltaX != 0 && deltaZ != 0 &&
                    (!snapshot.Walkable(from.X + deltaX, from.Y, from.Z) ||
                     !snapshot.Walkable(from.X, from.Y, from.Z + deltaZ))) {
                    return;
                }

                if (!snapshot.Walkable(candidate.X, candidate.Y, candidate.Z) ||
                    DistanceSquared(start, candidate) > maxDistanceSquared) {
                    return;
                }

                float score = scores[from] + MovementCost(from, candidate);
                if (scores.TryGetValue(candidate, out float existing) && score >= existing) {
                    return;
                }

                scores[candidate] = score;
                previous[candidate] = from;
                open.Enqueue(candidate, score + MathF.Sqrt(DistanceSquared(candidate, target)));
            }
        }

        return closest == start ? null : CreatePath(snapshot, previous, closest, target, false);
    }

    private static Path CreatePath(
        PathfindingSnapshot snapshot,
        Dictionary<PathNode, PathNode> previous,
        PathNode end,
        PathNode target,
        bool reached) {
        List<PathNode> nodes = [end];
        PathNode current = end;
        while (previous.TryGetValue(current, out PathNode parent)) {
            nodes.Add(parent);
            current = parent;
        }

        nodes.Reverse();
        return new Path(SmoothPath(snapshot, nodes), target, reached);
    }

    private static PathNode[] SmoothPath(PathfindingSnapshot snapshot, List<PathNode> nodes) {
        if (nodes.Count < 3) {
            return [.. nodes];
        }

        List<PathNode> smoothed = [nodes[0]];
        int anchor = 0;
        while (anchor < nodes.Count - 1) {
            int furthest = anchor + 1;
            for (int candidate = nodes.Count - 1; candidate > furthest; candidate--) {
                if (CanTraverse(snapshot, nodes[anchor], nodes[candidate])) {
                    furthest = candidate;
                    break;
                }
            }

            smoothed.Add(nodes[furthest]);
            anchor = furthest;
        }

        return [.. smoothed];
    }

    private static bool CanTraverse(PathfindingSnapshot snapshot, PathNode from, PathNode to) {
        if (from.Y != to.Y) {
            return false;
        }

        int deltaX = to.X - from.X;
        int deltaZ = to.Z - from.Z;
        int steps = Math.Max(Math.Abs(deltaX), Math.Abs(deltaZ));
        for (int step = 1; step <= steps; step++) {
            float progress = step / (float)steps;
            int x = (int)MathF.Round(from.X + deltaX * progress);
            int z = (int)MathF.Round(from.Z + deltaZ * progress);
            if (!snapshot.Walkable(x, from.Y, z)) {
                return false;
            }

            int previousX = (int)MathF.Round(from.X + deltaX * ((step - 1) / (float)steps));
            int previousZ = (int)MathF.Round(from.Z + deltaZ * ((step - 1) / (float)steps));
            if (x != previousX && z != previousZ &&
                (!snapshot.Walkable(x, from.Y, previousZ) ||
                 !snapshot.Walkable(previousX, from.Y, z))) {
                return false;
            }
        }

        return true;
    }

    private static float MovementCost(PathNode from, PathNode to) {
        return from.Y != to.Y
            ? 1f
            : from.X != to.X && from.Z != to.Z
                ? DiagonalDistance
                : 1f;
    }

    private static float DistanceSquared(PathNode from, PathNode to) {
        int x = from.X - to.X;
        int y = from.Y - to.Y;
        int z = from.Z - to.Z;
        return x * x + y * y + z * z;
    }
}
