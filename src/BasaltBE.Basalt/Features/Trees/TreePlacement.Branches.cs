using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Features;

internal sealed partial class TreePlacement {
    private void BuildMegaJungle(
        int x,
        int y,
        int z,
        int height) {
        AddBase(x, y, z, 2);
        for (int offsetY = 0; offsetY < height; offsetY++) {
            AddTrunk(x, y + offsetY, z, "y");
            if (offsetY < height - 1) {
                AddTrunk(x + 1, y + offsetY, z, "y");
                AddTrunk(x, y + offsetY, z + 1, "y");
                AddTrunk(x + 1, y + offsetY, z + 1, "y");
            }
        }

        List<FoliageAttachment> foliage = [
            new FoliageAttachment(x, y + height, z, 0, true)
        ];

        for (int branchHeight = height - 2 - random.Next(4);
            branchHeight > height / 2;
            branchHeight -= 2 + random.Next(4)) {
            float angle = random.NextSingle() * 2f * MathF.PI;
            int branchX = 0;
            int branchZ = 0;
            for (int branch = 0; branch < 5; branch++) {
                branchX = (int)(1.5f + MathF.Cos(angle) * branch);
                branchZ = (int)(1.5f + MathF.Sin(angle) * branch);
                AddTrunk(x + branchX,
                    y + branchHeight - 3 + branch / 2,
                    z + branchZ,
                    "y");
            }

            foliage.Add(new FoliageAttachment(
                x + branchX,
                y + branchHeight,
                z + branchZ,
                -2,
                false));
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            int foliageHeight = attachment.DoubleTrunk ? 2 : 1 + random.Next(2);
            for (int offsetY = 0; offsetY >= -foliageHeight; offsetY--) {
                int radius = 2 + attachment.RadiusOffset + 1 - offsetY;
                AddLeafRow(attachment.X,
                    attachment.Y + offsetY,
                    attachment.Z,
                    radius,
                    attachment.DoubleTrunk,
                    (_, _, dx, dz) =>
                        dx + dz >= 7 ||
                        dx * dx + dz * dz > radius * radius);
            }
        }
    }

    private void BuildDarkOak(
        int x,
        int y,
        int z,
        int height) {
        AddBase(x, y, z, 2);
        (int directionX, int directionZ) = HorizontalDirection();
        var lean = trunkData.GetProperty("trunk_lean");
        int leanHeight = height - Sample(lean.GetProperty("lean_height"), 2);
        int leanSteps = Sample(lean.GetProperty("lean_steps"), 1);
        int trunkX = x;
        int trunkZ = z;
        int attachmentY = y + height - 1;

        for (int offsetY = 0; offsetY < height; offsetY++) {
            if (offsetY >= leanHeight && leanSteps > 0) {
                trunkX += directionX;
                trunkZ += directionZ;
                leanSteps--;
            }

            AddTrunk(trunkX, y + offsetY, trunkZ, "y");
            AddTrunk(trunkX + 1, y + offsetY, trunkZ, "y");
            AddTrunk(trunkX, y + offsetY, trunkZ + 1, "y");
            AddTrunk(trunkX + 1, y + offsetY, trunkZ + 1, "y");
        }

        List<FoliageAttachment> foliage = [
            new FoliageAttachment(trunkX, attachmentY, trunkZ, 0, true)
        ];

        for (int offsetX = -1; offsetX <= 2; offsetX++) {
            for (int offsetZ = -1; offsetZ <= 2; offsetZ++) {
                bool outsideTrunk =
                    offsetX < 0 || offsetX > 1 ||
                    offsetZ < 0 || offsetZ > 1;
                var branches = trunkData.GetProperty("branches");
                if (!outsideTrunk || !Roll(branches.GetProperty("branch_chance"), 1.0 / 3)) {
                    continue;
                }

                int length = Sample(branches.GetProperty("branch_length"), 3);
                for (int branchY = 0; branchY < length; branchY++) {
                    AddTrunk(x + offsetX,
                        attachmentY - branchY - 1,
                        z + offsetZ,
                        "y");
                }

                foliage.Add(new FoliageAttachment(
                    x + offsetX,
                    attachmentY,
                    z + offsetZ,
                    0,
                    false));
            }
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            if (attachment.DoubleTrunk) {
                AddDarkOakLeafRow(attachment,
                    -1,
                    2);
                AddDarkOakLeafRow(attachment,
                    0,
                    3);
                AddDarkOakLeafRow(attachment,
                    1,
                    2);
                if (random.Next(2) == 0) {
                    AddDarkOakLeafRow(attachment,
                        2,
                        0);
                }
            }
            else {
                AddDarkOakLeafRow(attachment,
                    -1,
                    2);
                AddDarkOakLeafRow(attachment,
                    0,
                    1);
            }
        }
    }

    private void AddDarkOakLeafRow(
        FoliageAttachment attachment,
        int offsetY,
        int radius) {
        AddLeafRow(attachment.X,
            attachment.Y + offsetY,
            attachment.Z,
            radius,
            attachment.DoubleTrunk,
            (signedX, signedZ, dx, dz) => {
                if (offsetY == 0 &&
                    attachment.DoubleTrunk &&
                    (signedX == -radius || signedX >= radius) &&
                    (signedZ == -radius || signedZ >= radius)) {
                    return true;
                }

                if (offsetY == -1 && !attachment.DoubleTrunk) {
                    return dx == radius && dz == radius;
                }

                return offsetY == 1 && dx + dz > radius * 2 - 2;
            });
    }

    private void BuildAcacia(
        int x,
        int y,
        int z,
        int height) {
        AddBase(x, y, z, 1);
        List<FoliageAttachment> foliage = [];
        (int leanX, int leanZ) = HorizontalDirection();
        int leanHeight = height - random.Next(4) - 1;
        int leanSteps = 3 - random.Next(3);
        int trunkX = x;
        int trunkZ = z;
        int topY = y;

        for (int offsetY = 0; offsetY < height; offsetY++) {
            int blockY = y + offsetY;
            if (offsetY >= leanHeight && leanSteps > 0) {
                trunkX += leanX;
                trunkZ += leanZ;
                leanSteps--;
            }

            AddTrunk(trunkX, blockY, trunkZ, "y");
            topY = blockY + 1;
        }

        foliage.Add(new FoliageAttachment(trunkX, topY, trunkZ, 1, false));

        trunkX = x;
        trunkZ = z;
        (int branchX, int branchZ) = HorizontalDirection();
        if (branchX != leanX || branchZ != leanZ) {
            int branchStart = leanHeight - random.Next(2) - 1;
            int branchSteps = 1 + random.Next(3);
            int branchTop = int.MinValue;
            for (int offsetY = branchStart;
                offsetY < height && branchSteps > 0;
                offsetY++, branchSteps--) {
                if (offsetY < 1) {
                    continue;
                }

                trunkX += branchX;
                trunkZ += branchZ;
                int blockY = y + offsetY;
                AddTrunk(trunkX,
                    blockY,
                    trunkZ,
                    "y");
                branchTop = blockY + 1;
            }

            if (branchTop != int.MinValue) {
                foliage.Add(new FoliageAttachment(
                    trunkX,
                    branchTop,
                    trunkZ,
                    0,
                    false));
            }
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            int lowerRadius = 2 + attachment.RadiusOffset;
            AddLeafRow(attachment.X,
                attachment.Y - 1,
                attachment.Z,
                lowerRadius,
                false,
                (_, _, dx, dz) =>
                    dx == lowerRadius &&
                    dz == lowerRadius &&
                    lowerRadius > 0);
            AddLeafRow(attachment.X,
                attachment.Y,
                attachment.Z,
                1,
                false,
                (_, _, dx, dz) =>
                    (dx > 1 || dz > 1) &&
                    dx != 0 &&
                    dz != 0);
            int upperRadius = 1 + attachment.RadiusOffset;
            AddLeafRow(attachment.X,
                attachment.Y,
                attachment.Z,
                upperRadius,
                false,
                (_, _, dx, dz) =>
                    (dx > 1 || dz > 1) &&
                    dx != 0 &&
                    dz != 0);
        }
    }

    private void BuildFallen(
        int x,
        int y,
        int z,
        int length) {
        AddTrunk(x, y, z, "y");

        (int directionX, int directionZ) = HorizontalDirection();
        int logLength = length - 2;
        int gap = 2 + random.Next(2);
        int startX = x + directionX * gap;
        int startY = y + 1;
        int startZ = z + directionZ * gap;
        for (int fall = 0; fall < 6; fall++) {
            BlockPermutation current = dimension.GetPermutation(startX, startY, startZ);
            BlockPermutation below = dimension.GetPermutation(startX, startY - 1, startZ);
            if ((CanReplace(current) ||
                CanGrowThrough(current)) &&
                below.Identifier != "minecraft:air") {
                break;
            }

            startY--;
        }

        int gapInGround = 0;
        for (int step = 0; step < logLength; step++) {
            int blockX = startX + directionX * step;
            int blockZ = startZ + directionZ * step;
            BlockPermutation existing = dimension.GetPermutation(blockX, startY, blockZ);
            if (!CanReplace(existing) &&
                !CanGrowThrough(existing)) {
                return;
            }

            if ((BlockPalette.FindType(dimension.GetPermutation(blockX, startY - 1, blockZ).Identifier)?.Solid == true)) {
                gapInGround = 0;
            }
            else if (++gapInGround > 2) {
                return;
            }
        }

        for (int step = 0; step < logLength; step++) {
            AddTrunk(startX + directionX * step,
                startY,
                startZ + directionZ * step,
                directionX == 0 ? "z" : "x");
        }
    }
}
