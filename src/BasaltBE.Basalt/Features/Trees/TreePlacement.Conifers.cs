using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Features;

internal sealed partial class TreePlacement {
    private void BuildPine(
        int x,
        int y,
        int z,
        int height) {
        int foliageHeight = Sample(canopyData.GetProperty("canopy_height"), 4);
        int trunkHeight = height - foliageHeight;
        int leafRadius = 1 + random.Next(Math.Max(trunkHeight + 1, 1));
        const int foliageOffset = 1;

        AddBase(x, y, z, 1);
        AddVerticalTrunk(x, y, z, height, 1);

        int radius = 0;
        for (int offsetY = foliageOffset;
            offsetY >= foliageOffset - foliageHeight;
            offsetY--) {
            int currentRadius = radius;
            AddLeafRow(x,
                y + height + offsetY,
                z,
                currentRadius,
                false,
                (_, _, dx, dz) =>
                    dx == currentRadius &&
                    dz == currentRadius &&
                    currentRadius > 0);

            if (radius >= 1 &&
                offsetY == foliageOffset - foliageHeight + 1) {
                radius--;
            }
            else if (radius < leafRadius) {
                radius++;
            }
        }
    }

    private void BuildSpruce(
        int x,
        int y,
        int z,
        int height) {
        int foliageHeight = Math.Max(1, height - Sample(canopyData.GetProperty("lower_offset"), 2));
        int leafRadius = Sample(canopyData.GetProperty("max_radius"), 3);
        int foliageOffset = Sample(canopyData.GetProperty("upper_offset"), 0);

        AddBase(x, y, z, 1);
        AddVerticalTrunk(x, y, z, height, 1);

        int radius = random.Next(2);
        int maximumRadius = 1;
        int minimumRadius = 0;
        for (int offsetY = foliageOffset; offsetY >= -foliageHeight; offsetY--) {
            int currentRadius = radius;
            AddLeafRow(x,
                y + height + offsetY,
                z,
                currentRadius,
                false,
                (_, _, dx, dz) =>
                    dx == currentRadius &&
                    dz == currentRadius &&
                    currentRadius > 0);

            if (radius >= maximumRadius) {
                radius = minimumRadius;
                minimumRadius = 1;
                maximumRadius = Math.Min(maximumRadius + 1, leafRadius);
            }
            else {
                radius++;
            }
        }
    }

    private void BuildMegaPine(
        int x,
        int y,
        int z,
        int height) {
        int foliageHeight = Sample(canopyData.GetProperty("canopy_height"), 5);

        AddBase(x, y, z, 2);
        for (int offsetY = 0; offsetY < height; offsetY++) {
            AddTrunk(x, y + offsetY, z, "y");
            if (offsetY < height - 1) {
                AddTrunk(x + 1, y + offsetY, z, "y");
                AddTrunk(x, y + offsetY, z + 1, "y");
                AddTrunk(x + 1, y + offsetY, z + 1, "y");
            }
        }

        int previousRadius = 0;
        for (int absoluteY = Math.Max(y, y + height - foliageHeight);
            absoluteY <= y + height;
            absoluteY++) {
            int offset = y + height - absoluteY;
            int smoothRadius =
                (int)MathF.Floor((float)offset / foliageHeight * 3.5f);
            int radius = offset > 0 &&
                smoothRadius == previousRadius &&
                (absoluteY & 1) == 0
                ? smoothRadius + 1
                : smoothRadius;

            AddLeafRow(x,
                absoluteY,
                z,
                radius,
                true,
                (_, _, dx, dz) =>
                    dx + dz >= 7 ||
                    dx * dx + dz * dz > radius * radius);
            previousRadius = smoothRadius;
        }

        AddPodzolCircle(x - 1, y, z - 1);
        AddPodzolCircle(x + 2, y, z - 1);
        AddPodzolCircle(x - 1, y, z + 2);
        AddPodzolCircle(x + 2, y, z + 2);
        for (int i = 0; i < 5; i++) {
            int placement = random.Next(64);
            int offsetX = placement % 8;
            int offsetZ = placement / 8;
            if (offsetX == 0 ||
                offsetX == 7 ||
                offsetZ == 0 ||
                offsetZ == 7) {
                AddPodzolCircle(x - 3 + offsetX,
                    y,
                    z - 3 + offsetZ);
            }
        }
    }

    private void AddPodzolCircle(
        int centerX,
        int y,
        int centerZ) {
        BlockPermutation podzol = BlockPalette.FindDefaultPermutation("minecraft:podzol")!;
        for (int offsetX = -2; offsetX <= 2; offsetX++) {
            for (int offsetZ = -2; offsetZ <= 2; offsetZ++) {
                if (Math.Abs(offsetX) == 2 && Math.Abs(offsetZ) == 2) {
                    continue;
                }

                int blockX = centerX + offsetX;
                int blockZ = centerZ + offsetZ;
                for (int blockY = y + 2; blockY >= y - 3; blockY--) {
                    BlockPermutation existing = dimension.GetPermutation(blockX, blockY, blockZ);
                    if (CanGrowOn(existing)) {
                        placements[(blockX, blockY, blockZ)] =
                            new BlockPlacement(podzol, BlockKind.Base);
                        break;
                    }

                    if (existing.Identifier != "minecraft:air" &&
                        blockY < y) {
                        break;
                    }
                }
            }
        }
    }
}
