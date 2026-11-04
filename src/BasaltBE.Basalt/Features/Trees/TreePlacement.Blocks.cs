using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Features;

internal sealed partial class TreePlacement {
    private void AddBase(
        int x,
        int y,
        int z,
        int width) {
        for (int ox = 0; ox < width; ox++) {
            for (int oz = 0; oz < width; oz++) {
                BlockPermutation block = baseBlocks[(ox + oz) % baseBlocks.Length];
                placements[(x + ox, y - 1, z + oz)] =
                    new BlockPlacement(block, BlockKind.Base);
            }
        }
    }

    private void AddVerticalTrunk(
        int x,
        int y,
        int z,
        int height,
        int width) {
        for (int py = y; py < y + height; py++) {
            for (int ox = 0; ox < width; ox++) {
                for (int oz = 0; oz < width; oz++) {
                    AddTrunk(x + ox, py, z + oz, "y");
                }
            }
        }
    }

    private void AddTrunk(int x, int y, int z, string axis) {
        BlockPermutation permutation = axis == "x" ? trunkX : axis == "z" ? trunkZ : trunk;
        placements[(x, y, z)] = new BlockPlacement(permutation, BlockKind.Trunk);
    }

    private void AddLeafRow(
        int centerX,
        int y,
        int centerZ,
        int radius,
        bool doubleTrunk,
        Func<int, int, int, int, bool> skip) {
        if (radius < 0) {
            return;
        }

        int extra = doubleTrunk ? 1 : 0;
        for (int offsetX = -radius; offsetX <= radius + extra; offsetX++) {
            for (int offsetZ = -radius; offsetZ <= radius + extra; offsetZ++) {
                int distanceX = doubleTrunk
                    ? Math.Min(Math.Abs(offsetX), Math.Abs(offsetX - 1))
                    : Math.Abs(offsetX);
                int distanceZ = doubleTrunk
                    ? Math.Min(Math.Abs(offsetZ), Math.Abs(offsetZ - 1))
                    : Math.Abs(offsetZ);
                if (skip(offsetX, offsetZ, distanceX, distanceZ)) {
                    continue;
                }

                var position = (centerX + offsetX, y, centerZ + offsetZ);
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
