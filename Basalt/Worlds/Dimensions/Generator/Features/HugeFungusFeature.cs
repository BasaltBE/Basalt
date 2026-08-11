using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Protocol.Enums;

namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class HugeFungusFeature {
    private readonly string _baseBlock;
    private readonly BlockPermutation _stem;
    private readonly BlockPermutation _hat;
    private readonly BlockPermutation _decor;

    public readonly string Identifier;

    public HugeFungusFeature(
        string identifier,
        string baseBlock,
        string stem,
        string hat,
        string decor) {
        Identifier = identifier;
        _baseBlock = baseBlock;
        _stem = BlockPermutation.Resolve(stem);
        _hat = BlockPermutation.Resolve(hat);
        _decor = BlockPermutation.Resolve(decor);
    }

    public bool Populate(
        Dimension dimension,
        int x,
        int y,
        int z,
        Random? random = null,
        bool broadcast = true) {
        ArgumentNullException.ThrowIfNull(dimension);

        if (dimension.GetPermutation(x, y - 1, z).Type.Identifier != _baseBlock) {
            return false;
        }

        Random source = random ?? Random.Shared;
        int height = source.Next(4, 14);
        if (source.Next(12) == 0) {
            height *= 2;
        }

        Dictionary<(int X, int Y, int Z), BlockPermutation> placements = [];
        for (int offsetY = 0; offsetY < height; offsetY++) {
            int blockY = y + offsetY;
            if (Replaceable(dimension, placements, x, blockY, z, true)) {
                placements[(x, blockY, z)] = _stem;
            }
        }

        int hatHeight = Math.Min(source.Next(1 + height / 3) + 5, height);
        int hatStart = height - hatHeight;
        bool weepingVines = _hat.Type.Identifier == "minecraft:nether_wart_block";
        for (int offsetY = hatStart; offsetY <= height; offsetY++) {
            int radius = offsetY < height - source.Next(3) ? 2 : 1;
            if (hatHeight > 8 && offsetY < hatStart + 4) {
                radius = 3;
            }

            for (int offsetX = -radius; offsetX <= radius; offsetX++) {
                for (int offsetZ = -radius; offsetZ <= radius; offsetZ++) {
                    int blockX = x + offsetX;
                    int blockY = y + offsetY;
                    int blockZ = z + offsetZ;
                    if (!Replaceable(
                        dimension,
                        placements,
                        blockX,
                        blockY,
                        blockZ,
                        false)) {
                        continue;
                    }

                    bool edgeX = Math.Abs(offsetX) == radius;
                    bool edgeZ = Math.Abs(offsetZ) == radius;
                    bool inside = !edgeX && !edgeZ && offsetY != height;
                    bool corner = edgeX && edgeZ;
                    bool bottom = offsetY < hatStart + 3;
                    if (bottom) {
                        if (!inside) {
                            PlaceDropBlock(
                                dimension,
                                placements,
                                blockX,
                                blockY,
                                blockZ,
                                source,
                                weepingVines);
                        }
                    }
                    else if (inside) {
                        PlaceHatBlock(
                            dimension,
                            placements,
                            blockX,
                            blockY,
                            blockZ,
                            source,
                            0.1f,
                            0.2f,
                            weepingVines ? 0.1f : 0f);
                    }
                    else if (corner) {
                        PlaceHatBlock(
                            dimension,
                            placements,
                            blockX,
                            blockY,
                            blockZ,
                            source,
                            0.01f,
                            0.7f,
                            weepingVines ? 0.083f : 0f);
                    }
                    else {
                        PlaceHatBlock(
                            dimension,
                            placements,
                            blockX,
                            blockY,
                            blockZ,
                            source,
                            0.0005f,
                            0.98f,
                            weepingVines ? 0.07f : 0f);
                    }
                }
            }
        }

        int minY = dimension.Type == DimensionId.Overworld ? -64 : 0;
        int maxY = minY + Chunk.Chunk.MaxSubChunks * 16 - 1;
        foreach (((int _, int blockY, int _), _) in placements) {
            if (blockY < minY || blockY > maxY) {
                return false;
            }
        }

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

    private void PlaceDropBlock(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z,
        Random random,
        bool vines) {
        BlockPermutation below = Resolve(dimension, placements, x, y - 1, z);
        if (below.Type.Identifier == _hat.Type.Identifier ||
            random.NextSingle() < 0.15f) {
            placements[(x, y, z)] = _hat;
            if (vines && random.Next(11) == 0) {
                PlaceWeepingVines(dimension, placements, x, y, z, random);
            }
        }
    }

    private void PlaceHatBlock(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z,
        Random random,
        float decorChance,
        float hatChance,
        float vineChance) {
        if (random.NextSingle() < decorChance) {
            placements[(x, y, z)] = _decor;
        }
        else if (random.NextSingle() < hatChance) {
            placements[(x, y, z)] = _hat;
            if (random.NextSingle() < vineChance) {
                PlaceWeepingVines(dimension, placements, x, y, z, random);
            }
        }
    }

    private static void PlaceWeepingVines(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z,
        Random random) {
        if (Resolve(dimension, placements, x, y - 1, z).Type.Identifier !=
            "minecraft:air") {
            return;
        }

        int length = random.Next(1, 6);
        if (random.Next(7) == 0) {
            length *= 2;
        }

        for (int offset = 1; offset <= length; offset++) {
            int blockY = y - offset;
            if (Resolve(dimension, placements, x, blockY, z).Type.Identifier !=
                "minecraft:air") {
                break;
            }

            int age = offset == length ? random.Next(23, 26) : 0;
            placements[(x, blockY, z)] = BlockPermutation.Resolve(
                "minecraft:weeping_vines",
                new BlockState { ["weeping_vines_age"] = age });
        }
    }

    private static bool Replaceable(
        Dimension dimension,
        Dictionary<(int X, int Y, int Z), BlockPermutation> placements,
        int x,
        int y,
        int z,
        bool plants) {
        BlockPermutation block = Resolve(dimension, placements, x, y, z);
        if (block.Type.Air || !block.Type.Solid && !block.Type.Liquid) {
            return true;
        }

        return plants && block.Type.Identifier is
            "minecraft:crimson_fungus" or
            "minecraft:warped_fungus" or
            "minecraft:crimson_roots" or
            "minecraft:warped_roots";
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
}
