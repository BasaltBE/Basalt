using System.Text.Json;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Features;

internal sealed partial class TreePlacement {
    private void BuildStraightBlob(
        int x,
        int y,
        int z,
        int height,
        int radius) {
        AddBase(x, y, z, 1);
        AddVerticalTrunk(x, y, z, height, 1);

        JsonElement offsets = canopyData.GetProperty("canopy_offset");
        int minimum = offsets.GetProperty("min").GetInt32();
        int maximum = offsets.GetProperty("max").GetInt32();
        for (int offset = maximum; offset >= minimum; offset--) {
            int layerRadius = Math.Max(radius - 1 - offset / 2, 0);
            AddLeafRow(x,
                y + height + offset,
                z,
                layerRadius,
                false,
                (_, _, dx, dz) =>
                    dx == layerRadius &&
                    dz == layerRadius &&
                    (random.Next(2) == 0 || offset == 0));
        }
    }

    private void BuildBush(
        int x,
        int y,
        int z) {
        AddBase(x, y, z, 1);
        AddVerticalTrunk(x, y, z, 1, 1);

        for (int offset = 1; offset >= -1; offset--) {
            int radius = 1 - offset;
            AddLeafRow(x,
                y + 1 + offset,
                z,
                radius,
                false,
                (_, _, dx, dz) =>
                    dx == radius &&
                    dz == radius &&
                    random.Next(2) == 0);
        }
    }

    private void BuildFancy(
        int x,
        int y,
        int z,
        int treeHeight) {
        AddBase(x, y, z, 1);
        int height = treeHeight + 2;
        int trunkHeight = (int)Math.Floor(height * trunkData.GetProperty("trunk_height").GetProperty("scale").GetDouble());
        int trunkTop = y + trunkHeight;
        int relativeY = height - 5;
        List<(int X, int Y, int Z, int BranchBase)> foliage = [
            (x, y + relativeY, z, trunkTop)
        ];

        for (; relativeY >= 0; relativeY--) {
            float shape = FancyTreeShape(height, relativeY);
            if (shape < 0f) {
                continue;
            }

            double radius = shape * (random.NextSingle() + 0.328D);
            double angle = random.NextSingle() * 2f * Math.PI;
            int nodeX = x + (int)Math.Floor(radius * Math.Sin(angle) + 0.5D);
            int nodeY = y + relativeY - 1;
            int nodeZ = z + (int)Math.Floor(radius * Math.Cos(angle) + 0.5D);

            if (!FancyLimb(nodeX,
                nodeY,
                nodeZ,
                nodeX,
                nodeY + 5,
                nodeZ,
                false)) {
                continue;
            }

            int dx = x - nodeX;
            int dz = z - nodeZ;
            double branchHeight =
                nodeY - Math.Sqrt(dx * dx + dz * dz) * trunkData.GetProperty("branches").GetProperty("slope").GetDouble();
            int branchBase = branchHeight > trunkTop
                ? trunkTop
                : (int)branchHeight;

            if (!FancyLimb(x,
                branchBase,
                z,
                nodeX,
                nodeY,
                nodeZ,
                false)) {
                continue;
            }

            foliage.Add((nodeX, nodeY, nodeZ, branchBase));
        }

        FancyLimb(x,
            y,
            z,
            x,
            y + trunkHeight,
            z,
            true);

        for (int i = 0; i < foliage.Count; i++) {
            (int nodeX, int nodeY, int nodeZ, int branchBase) = foliage[i];
            if (branchBase - y < height * 0.2D) {
                continue;
            }

            if (branchBase != nodeY || nodeX != x || nodeZ != z) {
                FancyLimb(x,
                    branchBase,
                    z,
                    nodeX,
                    nodeY,
                    nodeZ,
                    true);
            }

            AddFancyFoliage(nodeX, nodeY, nodeZ);
        }
    }

    private float FancyTreeShape(int height, int y) {
        if (y < height * trunkData.GetProperty("foliage_altitude_factor").GetDouble()) {
            return -1f;
        }

        float radius = height / 2f;
        float adjacent = radius - y;
        if (Math.Abs(adjacent) >= radius) {
            return 0f;
        }

        float distance = adjacent == 0f
            ? radius
            : MathF.Sqrt(radius * radius - adjacent * adjacent);
        return distance * 0.5f;
    }

    private bool FancyLimb(
        int startX,
        int startY,
        int startZ,
        int endX,
        int endY,
        int endZ,
        bool place) {
        if (startX == endX && startY == endY && startZ == endZ) {
            if (place)
                AddTrunk(startX, startY, startZ, "y");
            return true;
        }

        int deltaX = endX - startX;
        int deltaY = endY - startY;
        int deltaZ = endZ - startZ;
        int steps = Math.Max(
            Math.Abs(deltaX),
            Math.Max(Math.Abs(deltaY), Math.Abs(deltaZ)));
        float stepX = (float)deltaX / steps;
        float stepY = (float)deltaY / steps;
        float stepZ = (float)deltaZ / steps;

        for (int step = 0; step <= steps; step++) {
            int x = startX + (int)MathF.Floor(0.5f + step * stepX);
            int y = startY + (int)MathF.Floor(0.5f + step * stepY);
            int z = startZ + (int)MathF.Floor(0.5f + step * stepZ);

            if (!place) {
                BlockPermutation existing = dimension.GetPermutation(x, y, z);
                if (!CanReplace(existing) &&
                    !CanGrowThrough(existing) &&
                    !existing.Identifier.EndsWith("_log", StringComparison.Ordinal)) {
                    return false;
                }

                continue;
            }

            int xDifference = Math.Abs(x - startX);
            int zDifference = Math.Abs(z - startZ);
            int maxDifference = Math.Max(xDifference, zDifference);
            string axis = maxDifference == 0
                ? "y"
                : xDifference == maxDifference ? "x" : "z";
            AddTrunk(x, y, z, axis);
        }

        return true;
    }

    private void AddFancyFoliage(
        int centerX,
        int centerY,
        int centerZ) {

        int canopyHeight = Math.Clamp(Number(canopyData, "height", 4), 1, 16);
        int canopyRadius = Math.Clamp(Number(canopyData, "radius", 3), 1, 16);
        for (int offsetY = canopyHeight; offsetY >= 0; offsetY--) {
            int radius = offsetY == canopyHeight || offsetY == 0 ? Math.Max(1, canopyRadius - 1) : canopyRadius;
            for (int offsetX = -radius; offsetX <= radius; offsetX++) {
                for (int offsetZ = -radius; offsetZ <= radius; offsetZ++) {
                    int absoluteX = Math.Abs(offsetX);
                    int absoluteZ = Math.Abs(offsetZ);
                    if ((absoluteX + 0.5f) * (absoluteX + 0.5f) +
                        (absoluteZ + 0.5f) * (absoluteZ + 0.5f) >
                        radius * radius) {
                        continue;
                    }

                    var position = (
                        centerX + offsetX,
                        centerY + offsetY,
                        centerZ + offsetZ);
                    if (placements.TryGetValue(position, out BlockPlacement existing) &&
                        existing.Kind == BlockKind.Trunk) {
                        continue;
                    }

                    placements[position] =
                        new BlockPlacement(leaf, BlockKind.Leaf);
                }
            }
        }
    }
}
