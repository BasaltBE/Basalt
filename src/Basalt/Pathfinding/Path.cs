namespace Basalt.Core.Pathfinding;

public sealed class Path {
    private readonly PathNode[] _nodes;

    public IReadOnlyList<PathNode> Nodes => _nodes;
    public PathNode Target { get; }
    public bool Reached { get; }

    internal Path(PathNode[] nodes, PathNode target, bool reached) {
        _nodes = nodes;
        Target = target;
        Reached = reached;
    }
}
