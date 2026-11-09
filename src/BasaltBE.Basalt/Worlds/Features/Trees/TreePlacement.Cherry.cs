using System.Text.Json;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Worlds.Features;

internal sealed partial class TreePlacement {
    private void BuildCherry(
        int x,
        int y,
        int z,
        int height) {
        AddBase(x, y, z, 1);
        JsonElement branches = trunkData.GetProperty("branches");
        int firstBranchOffset = Math.Max(0, height - 1 + Sample(branches.GetProperty("branch_start_offset_from_top"), -3));
        int secondBranchOffset = Math.Max(0, height - 1 + Sample(branches.GetProperty("branch_start_offset_from_top"), -4));
        if (secondBranchOffset >= firstBranchOffset) {
            secondBranchOffset++;
        }

        JsonElement weights = branches.GetProperty("tree_type_weights");
        int oneBranch = weights.GetProperty("one_branch").GetInt32();
        int twoBranches = weights.GetProperty("two_branches").GetInt32();
        int withTrunk = weights.GetProperty("two_branches_and_trunk").GetInt32();
        int selected = random.Next(checked(oneBranch + twoBranches + withTrunk));
        bool middleBranch = selected >= oneBranch + twoBranches;
        bool sideBranches = selected >= oneBranch;
        int trunkHeight = middleBranch
            ? height
            : sideBranches
                ? Math.Max(firstBranchOffset, secondBranchOffset) + 1
                : firstBranchOffset + 1;
        AddVerticalTrunk(x, y, z, trunkHeight, 1);

        List<FoliageAttachment> foliage = [];
        if (middleBranch) {
            foliage.Add(new FoliageAttachment(
                x,
                y + trunkHeight,
                z,
                0,
                false));
        }

        (int directionX, int directionZ) = HorizontalDirection();
        foliage.Add(BuildCherryBranch(x,
            y,
            z,
            height,
            firstBranchOffset,
            firstBranchOffset < trunkHeight - 1,
            directionX,
            directionZ));
        if (sideBranches) {
            foliage.Add(BuildCherryBranch(x,
                y,
                z,
                height,
                secondBranchOffset,
                secondBranchOffset < trunkHeight - 1,
                -directionX,
                -directionZ));
        }

        for (int i = 0; i < foliage.Count; i++) {
            AddCherryFoliage(foliage[i]);
        }
    }

    private FoliageAttachment BuildCherryBranch(
        int originX,
        int originY,
        int originZ,
        int height,
        int branchOffset,
        bool trunkContinues,
        int directionX,
        int directionZ) {
        int branchX = originX;
        int branchY = originY + branchOffset;
        int branchZ = originZ;
        JsonElement branches = trunkData.GetProperty("branches");
        int endOffset = height - 1 + Sample(branches.GetProperty("branch_end_offset_from_top"), 0);
        bool extends = trunkContinues || endOffset < branchOffset;
        int distance = Sample(branches.GetProperty("branch_horizontal_length"), 3) + (extends ? 1 : 0);
        int endX = originX + directionX * distance;
        int endY = originY + endOffset;
        int endZ = originZ + directionZ * distance;
        string horizontalAxis = directionX == 0 ? "z" : "x";

        int horizontalSteps = extends ? 2 : 1;
        for (int step = 0; step < horizontalSteps; step++) {
            branchX += directionX;
            branchZ += directionZ;
            AddTrunk(branchX,
                branchY,
                branchZ,
                horizontalAxis);
        }

        while (branchX != endX || branchY != endY || branchZ != endZ) {
            int remaining =
                Math.Abs(endX - branchX) +
                Math.Abs(endY - branchY) +
                Math.Abs(endZ - branchZ);
            float verticalChance =
                (float)Math.Abs(endY - branchY) / remaining;
            if (random.NextSingle() < verticalChance) {
                branchY += Math.Sign(endY - branchY);
                AddTrunk(branchX, branchY, branchZ, "y");
            } else {
                branchX += directionX;
                branchZ += directionZ;
                AddTrunk(branchX,
                    branchY,
                    branchZ,
                    horizontalAxis);
            }
        }

        return new FoliageAttachment(
            endX,
            endY + 1,
            endZ,
            0,
            false);
    }

    private void AddCherryFoliage(
        FoliageAttachment attachment) {
        int height = Math.Clamp(Number(canopyData, "height", 5), 3, 16);
        int radius = Math.Clamp(Number(canopyData, "radius", 4), 3, 16);
        int top = height / 2;
        int bottom = top - height + 1;
        float hanging = (float)canopyData.GetProperty("hanging_leaves_chance").GetDouble() / 100;
        float extension = (float)canopyData.GetProperty("hanging_leaves_extension_chance").GetDouble() / 100;
        for (int offset = top; offset >= bottom; offset--) {
            int layerRadius = offset == top ? radius - 3 : offset == top - 1 || offset == bottom ? radius - 2 : radius - 1;
            AddCherryLeafRow(attachment, offset, Math.Max(1, layerRadius));
            if (offset < 0) AddCherryHangingLeaves(attachment, offset, Math.Max(1, layerRadius), hanging, extension);
        }
    }

    private void AddCherryLeafRow(
        FoliageAttachment attachment,
        int offsetY,
        int radius) {
        AddLeafRow(attachment.X,
            attachment.Y + offsetY,
            attachment.Z,
            radius,
            false,
            (_, _, dx, dz) => {
                if (offsetY == -1 &&
                    (dx == radius || dz == radius) &&
                    random.NextSingle() < canopyData.GetProperty("wide_bottom_layer_hole_chance").GetDouble() / 100) {
                    return true;
                }

                bool corner = dx == radius && dz == radius;
                if (radius > 2) {
                    return corner ||
                        dx + dz > radius * 2 - 2 &&
                        random.NextSingle() < canopyData.GetProperty("corner_hole_chance").GetDouble() / 100;
                }

                return corner && random.NextSingle() < canopyData.GetProperty("corner_hole_chance").GetDouble() / 100;
            });
    }

    private void AddCherryHangingLeaves(
        FoliageAttachment attachment,
        int rowOffset,
        int radius,
        float chance,
        float extensionChance) {
        HashSet<(int X, int Z)> edge = [];
        for (int offset = -radius; offset < radius; offset++) {
            edge.Add((-radius, offset));
            edge.Add((radius, offset));
            edge.Add((offset, -radius));
            edge.Add((offset, radius));
        }

        foreach ((int offsetX, int offsetZ) in edge) {
            var above = (
                attachment.X + offsetX,
                attachment.Y + rowOffset,
                attachment.Z + offsetZ);
            if (!placements.TryGetValue(above, out BlockPlacement existing) ||
                existing.Kind != BlockKind.Leaf ||
                random.NextSingle() > chance) {
                continue;
            }

            var hanging = (above.Item1, above.Item2 - 1, above.Item3);
            int distance =
                Math.Abs(hanging.Item1 - attachment.X) +
                Math.Abs(hanging.Item2 - (attachment.Y - 1)) +
                Math.Abs(hanging.Item3 - attachment.Z);
            if (distance >= 7 || placements.ContainsKey(hanging)) {
                continue;
            }

            placements[hanging] =
                new BlockPlacement(leaf, BlockKind.Leaf);
            var extension = (
                hanging.Item1,
                hanging.Item2 - 1,
                hanging.Item3);
            if (random.NextSingle() <= extensionChance &&
                !placements.ContainsKey(extension)) {
                placements[extension] =
                    new BlockPlacement(leaf, BlockKind.Leaf);
            }
        }
    }
}
