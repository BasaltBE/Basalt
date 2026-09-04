namespace Basalt.Core.Tasks;

using Basalt.Core.Pathfinding;
using Basalt.Core.Worlds.Dimensions;

public sealed class PathfindingTask : ServerTask {
    private readonly Dimension _dimension;
    private readonly PathNode _start;
    private readonly PathNode _target;
    private readonly int _radius;
    private readonly int _verticalRange;
    private readonly int _maxVisitedNodes;
    private readonly float _maxDistance;
    private readonly Action<Path?> _completion;
    private Path? _path;

    public PathfindingTask(
        Dimension dimension,
        PathNode start,
        PathNode target,
        Action<Path?> completion,
        int radius = 32,
        int verticalRange = 8,
        int maxVisitedNodes = 4096,
        float maxDistance = 32f) {
        _dimension = dimension;
        _start = start;
        _target = target;
        _radius = radius;
        _verticalRange = verticalRange;
        _completion = completion;
        _maxVisitedNodes = maxVisitedNodes;
        _maxDistance = maxDistance;
        Priority = TaskPriority.Low;
    }

    public override void Execute() {
        PathfindingSnapshot snapshot = _dimension.CreatePathfindingSnapshot(
            _start,
            _target,
            _radius,
            _verticalRange);
        _path = GroundPathfinder.FindPath(
            snapshot,
            _start,
            _target,
            _maxVisitedNodes,
            _maxDistance);
    }

    public override void Complete() {
        _completion(_path);
    }
}
