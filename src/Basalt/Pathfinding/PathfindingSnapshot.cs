namespace Basalt.Core.Pathfinding;

public sealed class PathfindingSnapshot {
    private readonly bool[] _walkable;
    private readonly int _width;
    private readonly int _height;
    private readonly int _depth;

    internal PathfindingSnapshot(
        int minX,
        int minY,
        int minZ,
        int width,
        int height,
        int depth,
        bool[] walkable) {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        _width = width;
        _height = height;
        _depth = depth;
        _walkable = walkable;
    }

    public int MinX { get; }
    public int MinY { get; }
    public int MinZ { get; }
    public int MaxX => MinX + _width - 1;
    public int MaxY => MinY + _height - 1;
    public int MaxZ => MinZ + _depth - 1;

    public bool Walkable(int x, int y, int z) {
        if (x < MinX || x > MaxX || y < MinY || y > MaxY || z < MinZ || z > MaxZ) {
            return false;
        }

        int index = ((y - MinY) * _depth + z - MinZ) * _width + x - MinX;
        return _walkable[index];
    }
}
