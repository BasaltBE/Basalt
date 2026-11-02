namespace BasaltBE.Basalt.Blocks;

internal static class BlockDirections
{
    internal static readonly string[] ByFace = ["down", "up", "north", "south", "west", "east"];
    internal static readonly byte[] OppositeByFace = [1, 0, 3, 2, 5, 4];
    internal static readonly (int X, int Y, int Z)[] OffsetByFace = [
        (0, -1, 0),
        (0, 1, 0),
        (0, 0, -1),
        (0, 0, 1),
        (-1, 0, 0),
        (1, 0, 0)
    ];
    internal static readonly string[] ByYaw = ["south", "west", "north", "east"];
    internal static readonly string[] TorchFacingByFace = [string.Empty, "top", "south", "north", "east", "west"];
    internal static readonly string[] CardinalDirectionByYaw = ["north", "east", "south", "west"];
    internal static readonly int[] PistonFacingByYaw = [3, 4, 2, 5];
    internal static readonly (int X, int Y, int Z)[] PistonOffsetByFacing = [
        (0, -1, 0),
        (0, 1, 0),
        (0, 0, 1),
        (0, 0, -1),
        (1, 0, 0),
        (-1, 0, 0)
    ];
    internal static readonly Dictionary<string, (int X, int Y, int Z)> OffsetByCardinalDirection = new(StringComparer.Ordinal)
    {
        ["north"] = (0, 0, -1),
        ["east"] = (1, 0, 0),
        ["south"] = (0, 0, 1),
        ["west"] = (-1, 0, 0)
    };
    internal static readonly Dictionary<string, (int X, int Y, int Z)> OffsetByDirection = new(StringComparer.Ordinal)
    {
        ["down"] = (0, -1, 0),
        ["up"] = (0, 1, 0),
        ["north"] = (0, 0, -1),
        ["south"] = (0, 0, 1),
        ["west"] = (-1, 0, 0),
        ["east"] = (1, 0, 0)
    };
    internal static readonly Dictionary<string, (int X, int Y, int Z)> TorchSupportByDirection = new(StringComparer.Ordinal)
    {
        ["unknown"] = (0, -1, 0),
        ["top"] = (0, -1, 0),
        ["north"] = (0, 0, -1),
        ["south"] = (0, 0, 1),
        ["west"] = (-1, 0, 0),
        ["east"] = (1, 0, 0)
    };
    internal static readonly string[] FloorAxisByYaw = ["north_south", "east_west", "north_south", "east_west"];
    internal static readonly (int X, int Z)[] LeftByYaw = [(1, 0), (0, 1), (-1, 0), (0, -1)];
    internal static readonly int[] TrapdoorDirectionByYaw = [3, 0, 2, 1];
    internal static readonly int[] StairFacingByDirection = [1, 3, 2, 0];
    internal static readonly int[] StairOppositeByDirection = [1, 0, 3, 2];
    internal static readonly int[] StairOffsetXByDirection = [1, -1, 0, 0];
    internal static readonly int[] StairOffsetZByDirection = [0, 0, 1, -1];

    internal static int FacingByRotation(float pitch, float yaw) {
        if (pitch > 45)
            return 1;
        if (pitch < -45)
            return 0;
        return PistonFacingByYaw[(int)MathF.Floor((yaw + 45) / 90) & 3];
    }
}
