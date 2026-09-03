namespace Basalt.Core.Tasks;

using Basalt.Core.Pathfinding;

public sealed class PathfindingTask : ServerTask {
    private readonly PathfindingSnapshot _snapshot;
    private readonly PathNode _start;
    private readonly PathNode _target;
    private readonly int _maxVisitedNodes;
    private readonly float _maxDistance;
    private readonly Action<Path?> _completion;
    private Path? _path;

    public PathfindingTask(
        PathfindingSnapshot snapshot,
        PathNode start,
        PathNode target,
        Action<Path?> completion,
        int maxVisitedNodes = 4096,
        float maxDistance = 32f) {
        _snapshot = snapshot;
        _start = start;
        _target = target;
        _completion = completion;
        _maxVisitedNodes = maxVisitedNodes;
        _maxDistance = maxDistance;
        Priority = TaskPriority.Low;
    }

    public override void Execute() {
        _path = GroundPathfinder.FindPath(
            _snapshot,
            _start,
            _target,
            _maxVisitedNodes,
            _maxDistance);
    }

    public override void Complete() {
        _completion(_path);
    }
}
