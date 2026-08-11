using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Protocol.Enums;

namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class MangroveTreeFeature {
    private static readonly (int X, int Z)[] Directions = [
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1)
    ];

    private readonly BlockPermutation _log =
        BlockPermutation.Resolve("minecraft:mangrove_log");
    private readonly BlockPermutation _leaves =
        BlockPermutation.Resolve("minecraft:mangrove_leaves");
    private readonly BlockPermutation _roots =
        BlockPermutation.Resolve("minecraft:mangrove_roots");
    private readonly BlockPermutation _muddyRoots =
        BlockPermutation.Resolve(
            "minecraft:muddy_mangrove_roots",
            new BlockState { ["pillar_axis"] = "y" });

    public readonly string Identifier = "minecraft:mangrove_tree_feature";

    public bool Populate(
        Dimension dimension,
        int x,
        int y,
        int z,
        Random? random = null,
        bool broadcast = true) {
        ArgumentNullException.ThrowIfNull(dimension);

        BlockPermutation ground = dimension.GetPermutation(x, y - 1, z);
        if (!ground.Type.Solid) {
            return false;
        }

        Random source = random ?? Random.Shared;
        bool tall = source.NextSingle() < 0.85f;
        int height = tall
            ? 4 + source.Next(2) + source.Next(10)
            : 2 + source.Next(2) + source.Next(5);
        int rootOffset = tall ? source.Next(3, 8) : source.Next(1, 4);
        int trunkY = y + rootOffset;
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements = [];

        PlaceRoots(dimension, placements, x, y, z, trunkY, source);
        List<FoliageAttachment> foliage = [];
        for (int offsetY = 0; offsetY < height; offsetY++) {
            int blockY = trunkY + offsetY;
            if (Replaceable(dimension, placements, x, blockY, z)) {
                placements[(x, blockY, z)] = _log;
            }

            if (offsetY < height - 1 && source.NextSingle() < 0.5f) {
                (int directionX, int directionZ) =
                    Directions[source.Next(Directions.Length)];
                int branchLength = source.Next(2);
                int branchStart = Math.Max(
                    0,
                    branchLength - source.Next(2) - 1);
                int branchSteps = tall
                    ? source.Next(1, 7)
                    : source.Next(1, 5);
                PlaceBranch(
                    dimension,
                    placements,
                    foliage,
                    x,
                    blockY,
                    z,
                    height,
                    directionX,
                    directionZ,
                    branchStart,
                    branchSteps);
            }

            if (offsetY == height - 1) {
                foliage.Add(new FoliageAttachment(
                    x,
                    blockY + 1,
                    z));
            }
        }

        for (int attachmentIndex = 0;
            attachmentIndex < foliage.Count;
            attachmentIndex++) {
            FoliageAttachment attachment = foliage[attachmentIndex];
            for (int attempt = 0; attempt < 70; attempt++) {
                int leafX =
                    attachment.X + source.Next(3) - source.Next(3);
                int leafY =
                    attachment.Y + source.Next(2) - source.Next(2);
                int leafZ =
                    attachment.Z + source.Next(3) - source.Next(3);
                if (Replaceable(
                    dimension,
                    placements,
                    leafX,
                    leafY,
                    leafZ)) {
                    placements[(leafX, leafY, leafZ)] = _leaves;
                }
            }
        }

        AddDecorators(dimension, placements, source);

        int minY = dimension.Type == DimensionId.Overworld ? -64 : 0;
        int maxY = minY + Chunk.Chunk.MaxSubChunks * 16 - 1;
        foreach (((int _, int blockY, int _), _) in placements) {
            if (blockY < minY || blockY > maxY) {
                return false;
            }
        }

        dimension.SetPermutation(
            x,
            y,
            z,
            BlockPermutation.Resolve("minecraft:air"),
            broadcast: broadcast);
        foreach (((int blockX, int blockY, int blockZ), BlockPermutation block)
            in placements) {
            dimension.SetPermutation(
                blockX,
                blockY,
                blockZ,
                block,
                broadcast: broadcast);
        }

        return true;
    }

    private void PlaceRoots(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int originX,
        int originY,
        int originZ,
        int trunkY,
        Random random) {
        HashSet<(int X, int Y, int Z)> roots = [
            (originX, trunkY - 1, originZ)
        ];

        for (int directionIndex = 0;
            directionIndex < Directions.Length;
            directionIndex++) {
            (int directionX, int directionZ) = Directions[directionIndex];
            var start = (
                originX + directionX,
                trunkY,
                originZ + directionZ);
            roots.Add(start);
            List<(int X, int Y, int Z)> directionRoots = [];
            SimulateRoots(
                dimension,
                placements,
                directionRoots,
                start,
                directionX,
                directionZ,
                originX,
                trunkY,
                originZ,
                random,
                0);
            roots.UnionWith(directionRoots);
        }

        foreach ((int rootX, int rootY, int rootZ) in roots) {
            BlockPermutation existing = Resolve(
                dimension,
                placements,
                rootX,
                rootY,
                rootZ);
            if (!RootReplaceable(existing)) {
                continue;
            }

            BlockPermutation root = existing.Type.Identifier is
                "minecraft:mud" or
                "minecraft:muddy_mangrove_roots"
                ? _muddyRoots
                : _roots;
            placements[(rootX, rootY, rootZ)] = root;
            if (random.NextSingle() < 0.5f &&
                Resolve(
                    dimension,
                    placements,
                    rootX,
                    rootY + 1,
                    rootZ).Type.Identifier == "minecraft:air") {
                placements[(rootX, rootY + 1, rootZ)] =
                    BlockPermutation.Resolve("minecraft:moss_carpet");
            }
        }
    }

    private void SimulateRoots(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        List<(int X, int Y, int Z)> roots,
        (int X, int Y, int Z) position,
        int directionX,
        int directionZ,
        int originX,
        int originY,
        int originZ,
        Random random,
        int layer) {
        if (layer == 15 || roots.Count > 15) {
            return;
        }

        int width =
            Math.Abs(position.X - originX) +
            Math.Abs(position.Y - originY) +
            Math.Abs(position.Z - originZ);
        var below = (position.X, position.Y - 1, position.Z);
        var outwardBelow = (
            position.X + directionX,
            position.Y - 1,
            position.Z + directionZ);
        var outward = (
            position.X + directionX,
            position.Y,
            position.Z + directionZ);
        Span<(int X, int Y, int Z)> candidates =
            stackalloc (int X, int Y, int Z)[2];
        int count = 1;

        if (width > 5 && width <= 8) {
            candidates[0] = below;
            if (random.NextSingle() < 0.2f) {
                candidates[1] = outwardBelow;
                count = 2;
            }
        }
        else if (width > 8 || random.NextSingle() < 0.2f) {
            candidates[0] = below;
        }
        else {
            candidates[0] = random.Next(2) == 0 ? outward : below;
        }

        for (int i = 0; i < count; i++) {
            (int rootX, int rootY, int rootZ) = candidates[i];
            BlockPermutation block = Resolve(
                dimension,
                placements,
                rootX,
                rootY,
                rootZ);
            if (!RootReplaceable(block) ||
                roots.Contains((rootX, rootY, rootZ))) {
                continue;
            }

            roots.Add((rootX, rootY, rootZ));
            SimulateRoots(
                dimension,
                placements,
                roots,
                (rootX, rootY, rootZ),
                directionX,
                directionZ,
                originX,
                originY,
                originZ,
                random,
                layer + 1);
        }
    }

    private void PlaceBranch(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        List<FoliageAttachment> foliage,
        int x,
        int y,
        int z,
        int height,
        int directionX,
        int directionZ,
        int branchStart,
        int branchSteps) {
        int branchX = x;
        int branchZ = z;
        int branchTop = y + branchStart;
        for (int branchIndex = branchStart;
            branchIndex < height && branchSteps > 0;
            branchIndex++, branchSteps--) {
            if (branchIndex < 1) {
                continue;
            }

            branchX += directionX;
            branchZ += directionZ;
            int branchY = y + branchIndex;
            if (Replaceable(
                dimension,
                placements,
                branchX,
                branchY,
                branchZ)) {
                placements[(branchX, branchY, branchZ)] = _log;
                branchTop = branchY + 1;
            }

            foliage.Add(new FoliageAttachment(
                branchX,
                branchY,
                branchZ));
        }

        if (branchTop - y > 1) {
            foliage.Add(new FoliageAttachment(
                branchX,
                branchTop,
                branchZ));
            foliage.Add(new FoliageAttachment(
                branchX,
                branchTop - 2,
                branchZ));
        }
    }

    private static void AddDecorators(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        Random random) {
        List<(int X, int Y, int Z)> leaves = [];
        foreach (((int x, int y, int z), BlockPermutation block) in placements) {
            if (block.Type.Identifier == "minecraft:mangrove_leaves") {
                leaves.Add((x, y, z));
            }
        }

        BlockPermutation vine = BlockPermutation.Resolve("minecraft:vine");
        HashSet<(int X, int Y, int Z)> propaguleBlacklist = [];
        for (int i = 0; i < leaves.Count; i++) {
            (int leafX, int leafY, int leafZ) = leaves[i];
            for (int directionIndex = 0;
                directionIndex < Directions.Length;
                directionIndex++) {
                if (random.NextSingle() >= 0.125f) {
                    continue;
                }

                (int directionX, int directionZ) = Directions[directionIndex];
                int vineX = leafX + directionX;
                int vineZ = leafZ + directionZ;
                for (int length = 0, vineY = leafY;
                    length <= 4;
                    length++, vineY--) {
                    if (Resolve(
                        dimension,
                        placements,
                        vineX,
                        vineY,
                        vineZ).Type.Identifier != "minecraft:air") {
                        break;
                    }

                    placements[(vineX, vineY, vineZ)] = vine;
                }
            }

            var propagule = (leafX, leafY - 1, leafZ);
            if (propaguleBlacklist.Contains(propagule) ||
                random.NextSingle() >= 0.14f ||
                Resolve(
                    dimension,
                    placements,
                    leafX,
                    leafY - 1,
                    leafZ).Type.Identifier != "minecraft:air" ||
                Resolve(
                    dimension,
                    placements,
                    leafX,
                    leafY - 2,
                    leafZ).Type.Identifier != "minecraft:air") {
                continue;
            }

            placements[propagule] = BlockPermutation.Resolve(
                "minecraft:mangrove_propagule",
                new BlockState {
                    ["hanging"] = true,
                    ["propagule_stage"] = random.Next(5)
                });
            for (int offsetX = -1; offsetX <= 1; offsetX++) {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                    propaguleBlacklist.Add((
                        propagule.Item1 + offsetX,
                        propagule.Item2,
                        propagule.Item3 + offsetZ));
                }
            }
        }
    }

    private static bool Replaceable(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z) {
        BlockPermutation block = Resolve(dimension, placements, x, y, z);
        return block.Type.Air ||
            !block.Type.Solid ||
            block.Type.Identifier is
                "minecraft:mangrove_roots" or
                "minecraft:mangrove_leaves";
    }

    private static bool RootReplaceable(BlockPermutation block) {
        return block.Type.Air ||
            !block.Type.Solid ||
            block.Type.Identifier is
                "minecraft:mud" or
                "minecraft:muddy_mangrove_roots";
    }

    private static BlockPermutation Resolve(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z) {
        return placements.GetValueOrDefault((x, y, z)) ??
            dimension.GetPermutation(x, y, z);
    }

    private readonly record struct FoliageAttachment(int X, int Y, int Z);
}
