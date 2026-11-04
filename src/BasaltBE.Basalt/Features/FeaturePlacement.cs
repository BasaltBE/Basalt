using System.Text.Json;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Features;

internal sealed class FeaturePlacement(FeatureRegistry registry, Dimension dimension, Random random) {
    const int MaximumDepth = 32;
    const int MaximumBlocks = 4096;
    readonly Dictionary<(int X, int Y, int Z), BlockPermutation> changed = [];
    int placed;

    public int Place(Feature feature, int x, int y, int z) {
        Place(feature, x, y, z, 0);
        foreach (((int blockX, int blockY, int blockZ), BlockPermutation permutation) in changed) {
            dimension.BroadcastBlockUpdate(blockX, blockY, blockZ, permutation);
            dimension.UpdateNeighbors(blockX, blockY, blockZ);
        }
        return placed;
    }

    bool Place(Feature feature, int x, int y, int z, int depth) {
        if (depth >= MaximumDepth || placed >= MaximumBlocks)
            return false;

        if (feature.Type.Length == 0) {
            FeatureContext context = new(dimension, random, MaximumBlocks - placed);
            if (!feature.Generate(context, x, y, z) || context.Invalid)
                return false;

            bool success = false;
            foreach (((int blockX, int blockY, int blockZ), BlockPermutation permutation) in context.Blocks) {
                if (dimension.GetPermutation(blockX, blockY, blockZ) == permutation ||
                    !dimension.SetPermutation(blockX, blockY, blockZ, permutation))
                    continue;
                changed[(blockX, blockY, blockZ)] = permutation;
                placed++;
                success = true;
            }
            return success;
        }

        JsonElement definition = feature.Data.GetProperty(feature.Type);
        switch (feature.Type) {
            case "minecraft:tree_feature":
                return PlaceTree(definition, x, y, z);
            case "minecraft:single_block_feature":
                return PlaceBlock(ReadWeightedBlock(definition.GetProperty("places_block")), x, y, z);
            case "minecraft:horizontal_tree_decoration_feature":
                return PlaceBlock(ReadWeightedBlock(definition.GetProperty("places_block")), x, y, z);
            case "minecraft:aggregate_feature": {
                bool success = false;
                foreach (JsonElement child in definition.GetProperty("features").EnumerateArray()) {
                    if (registry.Find(child.GetString()!) is { } childFeature && Place(childFeature, x, y, z, depth + 1))
                        success = true;
                    else if (definition.TryGetProperty("early_out", out JsonElement earlyOut) &&
                        earlyOut.GetString() == "first_failure")
                        return success;
                }
                return success;
            }
            case "minecraft:weighted_random_feature": {
                JsonElement choices = definition.GetProperty("features");
                int totalWeight = 0;
                foreach (JsonElement choice in choices.EnumerateArray())
                    if (registry.Find(choice[0].GetString()!) is not null)
                        totalWeight += choice[1].GetInt32();
                if (totalWeight <= 0)
                    return false;
                int selectedWeight = random.Next(totalWeight);
                foreach (JsonElement choice in choices.EnumerateArray()) {
                    if (registry.Find(choice[0].GetString()!) is not { } candidate)
                        continue;
                    selectedWeight -= choice[1].GetInt32();
                    if (selectedWeight < 0) {
                        return Place(candidate, x, y, z, depth + 1);
                    }
                }
                return false;
            }
            case "minecraft:scatter_feature": {
                string childIdentifier = definition.GetProperty("places_feature").GetString()!;
                JsonElement distribution = definition.TryGetProperty("distribution", out JsonElement value) ? value : definition;
                int iterations = distribution.TryGetProperty("iterations", out JsonElement iterationValue)
                    ? iterationValue.GetInt32()
                    : 1;
                if (iterations is < 1 or > MaximumBlocks)
                    return false;
                int numerator = 1;
                int denominator = 1;
                if (definition.TryGetProperty("scatter_chance", out JsonElement chance)) {
                    numerator = chance.GetProperty("numerator").GetInt32();
                    denominator = chance.GetProperty("denominator").GetInt32();
                    if (numerator < 0 || denominator <= 0 || numerator > denominator)
                        return false;
                }
                bool success = false;
                for (int index = 0; index < iterations && placed < MaximumBlocks; index++) {
                    if (numerator < denominator && random.Next(denominator) >= numerator)
                        continue;
                    int targetX = x + Coordinate(distribution, "x");
                    int targetZ = z + Coordinate(distribution, "z");
                    int targetY = Coordinate(distribution, "y", targetX, targetZ, y);
                    if (registry.Find(childIdentifier) is { } childFeature &&
                        Place(childFeature, targetX, targetY, targetZ, depth + 1))
                        success = true;
                }
                return success;
            }
            default:
                return false;
        }
    }

    bool PlaceTree(JsonElement definition, int x, int y, int z) {
        TreePlacement tree = new(definition, dimension, random);
        if (tree.Build(x, y, z) is not { } blocks || blocks.Count > MaximumBlocks - placed)
            return false;

        foreach (((int blockX, int blockY, int blockZ), TreePlacement.BlockPlacement block) in blocks) {
            if (dimension.GetPermutation(blockX, blockY, blockZ) == block.Permutation)
                continue;
            dimension.SetPermutation(blockX, blockY, blockZ, block.Permutation);
            changed[(blockX, blockY, blockZ)] = block.Permutation;
            placed++;
        }
        return blocks.Count > 0;
    }

    bool PlaceBlock(string? identifier, int x, int y, int z) {
        if (identifier is null || y < dimension.MinimumY || y > dimension.MaximumY)
            return false;
        BlockPermutation? permutation = BlockPalette.FindDefaultPermutation(identifier);
        if (permutation is null)
            return false;
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (current == permutation || !BlockPalette.Replaceable(current))
            return false;
        if (!dimension.SetPermutation(x, y, z, permutation))
            return false;

        changed[(x, y, z)] = permutation;
        placed++;
        return true;
    }

    int Coordinate(JsonElement distribution, string axis, int worldX = 0, int worldZ = 0, int fallback = 0) {
        if (!distribution.TryGetProperty(axis, out JsonElement coordinate))
            return 0;
        if (coordinate.ValueKind == JsonValueKind.Number)
            return coordinate.GetInt32();
        if (coordinate.ValueKind == JsonValueKind.String) {
            FeatureExpression expression = FeatureExpression.Parse(coordinate.GetString()!);
            Chunk? chunk = dimension.GetChunk(worldX >> 4, worldZ >> 4);
            return chunk is null
                ? fallback
                : expression.EvaluateHeightmap(chunk, dimension.MinimumY, dimension.MaximumY, worldX, worldZ);
        }
        if (coordinate.ValueKind != JsonValueKind.Object ||
            !coordinate.TryGetProperty("extent", out JsonElement extent) || extent.GetArrayLength() != 2)
            return 0;

        int minimum = extent[0].GetInt32();
        int maximum = extent[1].GetInt32();
        if (minimum > maximum)
            return 0;
        if (coordinate.TryGetProperty("distribution", out JsonElement kind) && kind.GetString() == "triangle")
            return (int)MathF.Floor((random.Next(minimum, maximum + 1) + random.Next(minimum, maximum + 1)) / 2f);
        return random.Next(minimum, maximum + 1);
    }

    static string? ReadWeightedBlock(JsonElement value) {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();
        if (value.ValueKind == JsonValueKind.Object) {
            if (value.TryGetProperty("name", out JsonElement name) && name.ValueKind == JsonValueKind.String)
                return name.GetString();
            if (value.TryGetProperty("block", out JsonElement block) && block.ValueKind == JsonValueKind.String)
                return block.GetString();
        }
        if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0) {
            if (value[0].ValueKind == JsonValueKind.String)
                return value[0].GetString();
            return ReadWeightedBlock(value[0]);
        }
        return null;
    }

}
