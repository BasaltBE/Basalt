using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo(
    "minecraft:water",
    "minecraft:flowing_water",
    "minecraft:lava",
    "minecraft:flowing_lava"
)]
public sealed class FluidTrait : BlockTrait {
    static readonly BlockPermutation?[] WaterLevels = new BlockPermutation?[16];
    static readonly BlockPermutation?[] LavaLevels = new BlockPermutation?[16];
    static readonly BlockPermutation WaterSource = BlockPalette.FindDefaultPermutation("minecraft:water")!;
    static readonly BlockPermutation LavaSource = BlockPalette.FindDefaultPermutation("minecraft:lava")!;

    static FluidTrait() {
        foreach (BlockPermutation permutation in BlockPalette.Permutations) {
            if (permutation.State.Values.GetValueOrDefault("liquid_depth") is not int level || (uint)level >= 16)
                continue;
            if (permutation.Identifier == "minecraft:flowing_water")
                WaterLevels[level] = permutation;
            else if (permutation.Identifier == "minecraft:flowing_lava")
                LavaLevels[level] = permutation;
        }
    }

    internal override bool HasScheduledUpdates => true;

    public override void OnPlaced(Dimension dimension, int x, int y, int z) =>
        dimension.ScheduleBlockUpdate(x, y, z, Delay(dimension, dimension.GetPermutation(x, y, z).Identifier));

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z, BlockPermutation neighbor) {
        BlockPermutation? block = dimension.GetLoadedPermutation(x, y, z);
        if (block is null || !Fluid(block.Identifier))
            return false;

        bool lava = block.Identifier is "minecraft:lava" or "minecraft:flowing_lava";
        if (Fluid(neighbor.Identifier) && !OtherFluid(neighbor.Identifier, lava)) {
            int currentLevel = block.State.Values.GetValueOrDefault("liquid_depth") is int level ? level : 0;
            int neighborLevel = neighbor.State.Values.GetValueOrDefault("liquid_depth") is int neighborValue ? neighborValue : 0;
            bool neighborSource = neighbor.Identifier == (lava ? "minecraft:lava" : "minecraft:water") && neighborLevel == 0;
            if (currentLevel == 0) {
                if (!neighborSource)
                    return false;
            }
            else if ((neighborLevel >= 8 ? 0 : neighborLevel) + (lava ? 2 : 1) > currentLevel) {
                return false;
            }
        }

        dimension.ScheduleBlockUpdate(x, y, z, Delay(dimension, block.Identifier));
        return false;
    }

    public override bool OnNeighborUpdate(
        Dimension dimension,
        int x,
        int y,
        int z,
        BlockPermutation previous,
        BlockPermutation neighbor
    ) {
        BlockPermutation? block = dimension.GetLoadedPermutation(x, y, z);
        if (block is null || !Fluid(block.Identifier))
            return false;

        bool lava = block.Identifier is "minecraft:lava" or "minecraft:flowing_lava";
        bool previousFluid = Fluid(previous.Identifier) && !OtherFluid(previous.Identifier, lava);
        bool neighborFluid = Fluid(neighbor.Identifier) && !OtherFluid(neighbor.Identifier, lava);
        if (neighborFluid) {
            int neighborLevel = neighbor.State.Values.GetValueOrDefault("liquid_depth") is int newLevel ? newLevel : 0;
            int currentLevel = block.State.Values.GetValueOrDefault("liquid_depth") is int level ? level : 0;
            bool previousSource = previousFluid &&
                previous.Identifier == (lava ? "minecraft:lava" : "minecraft:water") &&
                previous.State.Values.GetValueOrDefault("liquid_depth") is 0;
            bool neighborSource = neighbor.Identifier == (lava ? "minecraft:lava" : "minecraft:water") && neighborLevel == 0;
            if (!previousFluid) {
                if (currentLevel == 0) {
                    if (!neighborSource)
                        return false;
                }
                else if (!neighborSource && (neighborLevel >= 8 ? 0 : neighborLevel) + (lava ? 2 : 1) > currentLevel) {
                    return false;
                }
            }
            else {
                int previousLevel = previous.State.Values.GetValueOrDefault("liquid_depth") is int oldLevel ? oldLevel : 0;
                if (previousLevel == neighborLevel && previousSource == neighborSource)
                    return false;

                if (previousSource == neighborSource) {
                    if (currentLevel == 0)
                        return false;

                    int previousFlowLevel = previousLevel >= 8 ? 0 : previousLevel;
                    int neighborFlowLevel = neighborLevel >= 8 ? 0 : neighborLevel;
                    if (Math.Min(previousFlowLevel, neighborFlowLevel) + (lava ? 2 : 1) > currentLevel)
                        return false;
                }
            }
        }

        dimension.ScheduleBlockUpdate(x, y, z, Delay(dimension, block.Identifier));
        return false;
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        using ProfilerZone zone = Profiler.BeginZone("Fluid.ScheduledUpdate");
        if (!Fluid(current.Identifier))
            return;

        bool lava = current.Identifier is "minecraft:lava" or "minecraft:flowing_lava";
        int level = current.State.Values.GetValueOrDefault("liquid_depth") is int value ? value : 0;
        int drop = lava ? 2 : 1;

        if (level > 0) {
            int recalculatedLevel = int.MaxValue;
            int sources = 0;
            bool hasUnloadedNeighbor = false;

            for (int face = 2; face < 6; face++) {
                (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
                BlockPermutation? neighbor = dimension.GetLoadedPermutation(x + offsetX, y + offsetY, z + offsetZ);
                if (neighbor is null) {
                    hasUnloadedNeighbor = true;
                    continue;
                }
                if (!Fluid(neighbor.Identifier) || OtherFluid(neighbor.Identifier, lava))
                    continue;

                int neighborLevel = neighbor.State.Values.GetValueOrDefault("liquid_depth") is int neighborValue
                    ? neighborValue
                    : 0;
                if (neighbor.Identifier == (lava ? "minecraft:lava" : "minecraft:water"))
                    sources++;
                recalculatedLevel = Math.Min(recalculatedLevel, neighborLevel >= 8 ? 0 : neighborLevel);
            }

            BlockPermutation? above = dimension.GetLoadedPermutation(x, y + 1, z);
            bool fallingFromAbove = above is not null && Fluid(above.Identifier) && !OtherFluid(above.Identifier, lava);
            if (fallingFromAbove)
                recalculatedLevel = 8;
            else if (recalculatedLevel != int.MaxValue)
                recalculatedLevel += drop;

            if (!lava && sources >= 2 && Solid(dimension.GetLoadedPermutation(x, y - 1, z)))
                recalculatedLevel = 0;

            if (!hasUnloadedNeighbor || recalculatedLevel == 8) {
                if (recalculatedLevel > 7 && !fallingFromAbove) {
                    if (dimension.SetPermutation(x, y, z, BlockStorage.Air)) {
                        dimension.QueueFluidBlockUpdate(x, y, z, BlockStorage.Air);
                        dimension.UpdateNeighbors(x, y, z, current);
                    }
                    return;
                }

                if (recalculatedLevel != level) {
                    if (recalculatedLevel == 0 && !lava)
                        SetFluid(dimension, x, y, z, "minecraft:water", 0);
                    else
                        SetFluid(dimension, x, y, z, lava ? "minecraft:flowing_lava" : "minecraft:flowing_water", recalculatedLevel);
                    current = dimension.GetLoadedPermutation(x, y, z)!;
                    level = recalculatedLevel;
                }
            }
        }

        BlockPermutation? below = y > dimension.MinimumY ? dimension.GetLoadedPermutation(x, y - 1, z) : null;
        if (below is not null && Fluid(below.Identifier) && OtherFluid(below.Identifier, lava)) {
            if (lava)
                React(dimension, x, y, z, current);
            else
                React(dimension, x, y - 1, z, below);
            return;
        }
        if (below is not null && BlockPalette.Replaceable(below) &&
            !(below.Identifier == (lava ? "minecraft:lava" : "minecraft:water") &&
                below.State.Values.GetValueOrDefault("liquid_depth") is 0)) {
            Spread(dimension, x, y - 1, z, lava, 8);
            if (SourceCount(dimension, x, y, z, lava) >= 3)
                SpreadSides(dimension, x, y, z, level, lava);
            return;
        }

        bool waterHole = below is not null && BlockPalette.Replaceable(below);
        if (level == 0 || !waterHole)
            SpreadSides(dimension, x, y, z, level, lava);
    }

    static void SpreadSides(Dimension dimension, int x, int y, int z, int level, bool lava) {
        using ProfilerZone zone = Profiler.BeginZone("Fluid.SpreadSides");
        int nextLevel = level == 0 || level >= 8 ? (lava ? 2 : 1) : level + (lava ? 2 : 1);
        if (nextLevel > 7)
            return;

        int maximumDistance = lava ? (dimension.Type == DimensionType.Nether ? 4 : 2) : 4;
        int radius = maximumDistance + 1;
        int width = radius * 2 + 1;
        int cellCount = width * width;
        Span<byte> distances = stackalloc byte[cellCount];
        distances.Fill(byte.MaxValue);
        Span<byte> directions = stackalloc byte[cellCount];
        Span<byte> cells = stackalloc byte[cellCount];
        Span<int> queue = stackalloc int[cellCount * 4];
        int origin = radius * width + radius;
        bool hasUsefulSide = false;
        int tail = 0;

        for (int face = 2; face < 6; face++) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            int index = (radius + offsetX) * width + radius + offsetZ;
            BlockPermutation? block = dimension.GetLoadedPermutation(x + offsetX, y, z + offsetZ);
            if (!CanReceive(block, lava)) {
                cells[index] = 1;
                continue;
            }
            cells[index] = 3;

            if (!Fluid(block!.Identifier) || OtherFluid(block.Identifier, lava) ||
                block.Identifier is "minecraft:flowing_water" or "minecraft:flowing_lava" &&
                block.State.Values.GetValueOrDefault("liquid_depth") is int targetLevel && targetLevel > nextLevel)
                hasUsefulSide = true;

            distances[index] = 1;
            directions[index] = (byte)(1 << (face - 2));
            queue[tail++] = index;
        }

        if (!hasUsefulSide)
            return;

        byte bestDirections = 0;
        int bestDistance = int.MaxValue;
        if (y > dimension.MinimumY) {
            int head = 0;
            while (head < tail) {
                int index = queue[head++];
                int distance = distances[index];
                int dropDistance = distance - 1;
                if (dropDistance > maximumDistance || dropDistance > bestDistance)
                    continue;

                int cellX = index / width;
                int cellZ = index % width;
                BlockPermutation? belowBlock = dimension.GetLoadedPermutation(
                    x + cellX - radius, y - 1, z + cellZ - radius
                );
                if (belowBlock is not null && BlockPalette.Replaceable(belowBlock)) {
                    if (dropDistance < bestDistance) {
                        bestDistance = dropDistance;
                        bestDirections = directions[index];
                    }
                    else
                        bestDirections |= directions[index];
                }

                if (dropDistance == maximumDistance || dropDistance >= bestDistance)
                    continue;

                for (int face = 2; face < 6; face++) {
                    (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
                    int nextX = cellX + offsetX;
                    int nextZ = cellZ + offsetZ;
                    if ((uint)nextX >= (uint)width || (uint)nextZ >= (uint)width)
                        continue;

                    int nextIndex = nextX * width + nextZ;
                    if (nextIndex == origin)
                        continue;
                    byte nextDistance = (byte)(distance + 1);
                    if (distances[nextIndex] < nextDistance)
                        continue;

                    if ((cells[nextIndex] & 1) == 0) {
                        BlockPermutation? nextBlock = dimension.GetLoadedPermutation(
                            x + nextX - radius, y, z + nextZ - radius
                        );
                        cells[nextIndex] = (byte)((cells[nextIndex] & 0b11111100) |
                            (CanReceive(nextBlock, lava) ? 3 : 1));
                    }
                    if ((cells[nextIndex] & 2) == 0)
                        continue;

                    if (distances[nextIndex] == nextDistance) {
                        byte addedDirections = (byte)(directions[index] & ~directions[nextIndex]);
                        if (addedDirections == 0)
                            continue;
                        directions[nextIndex] |= addedDirections;
                    }
                    else {
                        distances[nextIndex] = nextDistance;
                        directions[nextIndex] = directions[index];
                    }
                    queue[tail++] = nextIndex;
                }
            }
        }

        for (int face = 2; face < 6; face++) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            int nextX = x + offsetX;
            int nextY = y + offsetY;
            int nextZ = z + offsetZ;
            BlockPermutation? target = dimension.GetLoadedPermutation(nextX, nextY, nextZ);
            int direction = 1 << (face - 2);
            if (!CanReceive(target, lava) || bestDistance != int.MaxValue && (bestDirections & direction) == 0)
                continue;

            if (target is not null && Fluid(target.Identifier)) {
                if (OtherFluid(target.Identifier, lava)) {
                    if (lava)
                        React(dimension, x, y, z, dimension.GetLoadedPermutation(x, y, z)!);
                    else
                        React(dimension, nextX, nextY, nextZ, target);
                }
                else if ((target.Identifier is "minecraft:flowing_water" or "minecraft:flowing_lava") &&
                    target.State.Values.GetValueOrDefault("liquid_depth") is int targetLevel && targetLevel > nextLevel) {
                    BlockPermutation? updated = (lava ? LavaLevels : WaterLevels)[nextLevel];
                    if (updated is not null)
                        SetFluid(dimension, nextX, nextY, nextZ, updated.Identifier, nextLevel, updated);
                }
            }
            else
                Spread(dimension, nextX, nextY, nextZ, lava, nextLevel);
        }
    }

    static bool CanReceive(BlockPermutation? block, bool lava) => block is not null &&
        BlockPalette.Replaceable(block) && !(block.Identifier == (lava ? "minecraft:lava" : "minecraft:water") &&
            block.State.Values.GetValueOrDefault("liquid_depth") is 0);

    static int SourceCount(Dimension dimension, int x, int y, int z, bool lava) {
        int sources = 0;
        for (int face = 2; face < 6; face++) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            BlockPermutation? neighbor = dimension.GetLoadedPermutation(x + offsetX, y + offsetY, z + offsetZ);
            if (neighbor?.Identifier == (lava ? "minecraft:lava" : "minecraft:water") &&
                neighbor.State.Values.GetValueOrDefault("liquid_depth") is 0)
                sources++;
        }
        return sources;
    }

    static void Spread(Dimension dimension, int x, int y, int z, bool lava, int level) {
        BlockPermutation? current = dimension.GetLoadedPermutation(x, y, z);
        if (current is null)
            return;
        if (Fluid(current.Identifier)) {
            if (OtherFluid(current.Identifier, lava))
                React(dimension, x, y, z, current);
            else if (current.Identifier is "minecraft:flowing_water" or "minecraft:flowing_lava" &&
                current.State.Values.GetValueOrDefault("liquid_depth") is int currentLevel &&
                (level == 8 ? currentLevel is > 0 and < 8 : currentLevel > level && currentLevel < 8)) {
                BlockPermutation? strongerFlowing = (lava ? LavaLevels : WaterLevels)[level];
                if (strongerFlowing is not null)
                    SetFluid(dimension, x, y, z, strongerFlowing.Identifier, level, strongerFlowing);
            }
            return;
        }
        if (!BlockPalette.Replaceable(current))
            return;

        BlockPermutation? flowing = (lava ? LavaLevels : WaterLevels)[level];
        if (flowing is null)
            return;
        SetFluid(dimension, x, y, z, flowing.Identifier, level, flowing);
    }

    static void React(Dimension dimension, int x, int y, int z, BlockPermutation fluid) {
        if (fluid.Identifier is not ("minecraft:lava" or "minecraft:flowing_lava"))
            return;

        string solid = fluid.Identifier == "minecraft:lava" &&
            fluid.State.Values.GetValueOrDefault("liquid_depth") is 0
                ? "minecraft:obsidian"
                : "minecraft:cobblestone";
        BlockPermutation? result = BlockPalette.FindDefaultPermutation(solid);
        if (result is not null)
            SetFluid(dimension, x, y, z, solid, 0, result);
    }

    static void SetFluid(Dimension dimension, int x, int y, int z, string identifier, int level, BlockPermutation? permutation = null) {
        using ProfilerZone zone = Profiler.BeginZone("Fluid.SetFluid");
        if (permutation is null) {
            if (identifier == "minecraft:water")
                permutation = WaterSource;
            else if (identifier == "minecraft:lava")
                permutation = LavaSource;
            else if ((uint)level < 16)
                permutation = (identifier == "minecraft:flowing_lava" ? LavaLevels : WaterLevels)[level];
        }
        BlockPermutation? previous = dimension.GetLoadedPermutation(x, y, z);
        if (permutation is null || previous is null || !dimension.SetPermutation(x, y, z, permutation))
            return;

        dimension.QueueFluidBlockUpdate(x, y, z, permutation);
        dimension.UpdateNeighbors(x, y, z, previous);
        dimension.ScheduleBlockUpdate(x, y, z, Delay(dimension, identifier));
    }

    static bool Solid(BlockPermutation? block) => block is not null &&
        BlockPalette.FindType(block.Identifier)?.Solid == true;

    static bool Fluid(string identifier) => identifier is
        "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava";

    static bool OtherFluid(string identifier, bool lava) => lava
        ? identifier is "minecraft:water" or "minecraft:flowing_water"
        : identifier is "minecraft:lava" or "minecraft:flowing_lava";

    static ulong Delay(Dimension dimension, string identifier) =>
        identifier is "minecraft:lava" or "minecraft:flowing_lava"
            ? (dimension.Type == DimensionType.Nether ? 10UL : 30UL)
            : 5UL;
}
