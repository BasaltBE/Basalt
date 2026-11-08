using System.Text.Json;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Worlds.Features;

internal sealed partial class TreePlacement(JsonElement definition, Dimension dimension, Random random) {
    static readonly string[] TrunkTypes = ["trunk", "fancy_trunk", "cherry_trunk", "mega_trunk", "acacia_trunk", "mangrove_trunk", "poplar_trunk", "fallen_trunk"];
    static readonly string[] CanopyTypes = ["canopy", "fancy_canopy", "cherry_canopy", "mega_canopy", "mega_pine_canopy", "pine_canopy", "spruce_canopy", "roofed_canopy", "acacia_canopy", "random_spread_canopy", "mangrove_canopy", "poplar_canopy"];
    readonly Dictionary<(int X, int Y, int Z), BlockPlacement> placements = [];
    BlockPermutation trunk = null!;
    BlockPermutation trunkX = null!;
    BlockPermutation trunkZ = null!;
    BlockPermutation leaf = null!;
    BlockPermutation[] baseBlocks = [];
    JsonElement trunkData;
    JsonElement canopyData;
    BlockRule[]? replaceRules;
    BlockRule[]? growThroughRules;
    BlockRule[]? groundRules;
    BlockRule[]? rootRules;
    (BlockPermutation Block, int Weight)[] leafBlocks = [];
    int leafWeight;

    internal Dictionary<(int X, int Y, int Z), BlockPlacement>? Build(int x, int y, int z) {
        string? trunkType = null;
        foreach (string type in TrunkTypes) {
            if (definition.TryGetProperty(type, out trunkData)) {
                trunkType = type;
                break;
            }
        }
        if (trunkType is null || Resolve(trunkData.GetProperty("trunk_block")) is not { } log) return null;

        trunk = log;
        trunkX = BlockPalette.Find(log.Identifier, log.State, "pillar_axis", "x") ?? log;
        trunkZ = BlockPalette.Find(log.Identifier, log.State, "pillar_axis", "z") ?? log;
        replaceRules = ReadRules(definition, "may_replace");
        growThroughRules = ReadRules(definition, "may_grow_through");
        groundRules = ReadRules(definition, "may_grow_on");
        if (definition.TryGetProperty("mangrove_roots", out JsonElement roots)) rootRules = ReadRules(roots, "roots_may_grow_through");
        string? canopyType = null;
        foreach (string type in CanopyTypes) {
            if (definition.TryGetProperty(type, out canopyData)) {
                canopyType = type;
                break;
            }
        }
        if (canopyType is not null) {
            JsonElement leaves = canopyData.TryGetProperty("leaf_block", out JsonElement block)
                ? block : canopyData.GetProperty("leaf_blocks");
            if (leaves.ValueKind == JsonValueKind.Array) {
                leafBlocks = new (BlockPermutation, int)[leaves.GetArrayLength()];
                for (int index = 0; index < leafBlocks.Length; index++) {
                    int weight = leaves[index][1].GetInt32();
                    if (weight <= 0 || Resolve(leaves[index][0]) is not { } resolved) return null;
                    leafBlocks[index] = (resolved, weight);
                    leafWeight = checked(leafWeight + weight);
                }
                if (leafBlocks.Length == 0) return null;
                leaf = SelectLeaf();
            } else if (Resolve(leaves) is { } resolved) {
                leaf = resolved;
                leafBlocks = [(resolved, 1)];
                leafWeight = 1;
            } else {
                return null;
            }
        }

        if (definition.TryGetProperty("base_block", out JsonElement bases)) {
            baseBlocks = bases.ValueKind == JsonValueKind.Array
                ? bases.EnumerateArray().Select(Resolve).OfType<BlockPermutation>().ToArray()
                : Resolve(bases) is { } baseBlock ? [baseBlock] : [];
        }
        if (baseBlocks.Length == 0) baseBlocks = [BlockPalette.FindDefaultPermutation("minecraft:dirt")!];

        if (y <= dimension.MinimumY || !CanGrowOn(dimension.GetPermutation(x, y - 1, z))) return null;

        int height = trunkType == "fallen_trunk"
            ? Sample(trunkData.GetProperty("log_length"), 4)
            : Sample(trunkData.GetProperty("trunk_height"), 5);
        if (trunkData.TryGetProperty("height_modifier", out JsonElement modifier)) height += Sample(modifier, 0);
        if (height is < 1 or > 64) return null;

        if (trunkType == "fallen_trunk") {
            BuildFallen(x, y, z, height);
        } else {
            switch (canopyType) {
                case "fancy_canopy":
                    BuildFancy(x, y, z, Math.Max(3, height - 2));
                    break;
                case "cherry_canopy":
                    BuildCherry(x, y, z, height);
                    break;
                case "roofed_canopy":
                    BuildDarkOak(x, y, z, height);
                    break;
                case "acacia_canopy":
                    BuildAcacia(x, y, z, height);
                    break;
                case "mega_canopy":
                    BuildMegaJungle(x, y, z, height);
                    break;
                case "mega_pine_canopy":
                    BuildMegaPine(x, y, z, height);
                    break;
                case "pine_canopy":
                    BuildPine(x, y, z, height);
                    break;
                case "spruce_canopy":
                    BuildSpruce(x, y, z, height);
                    break;
                case "random_spread_canopy":
                    BuildAzalea(x, y, z, height);
                    break;
                case "mangrove_canopy":
                    BuildMangrove(x, y, z, height);
                    break;
                case "poplar_canopy":
                    BuildPoplar(x, y, z, height);
                    break;
                case "canopy":
                    if (height == 1) BuildBush(x, y, z);
                    else BuildStraightBlob(x, y, z, height, Math.Max(2, Number(canopyData, "min_width", 1) + 1));
                    break;
                default:
                    return null;
            }
        }

        if (trunkData.TryGetProperty("trunk_decoration", out _) || canopyData.ValueKind == JsonValueKind.Object && canopyData.TryGetProperty("canopy_decoration", out _))
            AddVines();
        if (leaf?.Identifier == "minecraft:pale_oak_leaves") AddPaleMoss();

        foreach (((int blockX, int blockY, int blockZ), BlockPlacement block) in placements) {
            if (blockY < dimension.MinimumY || blockY > dimension.MaximumY) return null;

            BlockPermutation current = dimension.GetPermutation(blockX, blockY, blockZ);
            bool allowed = block.Kind == BlockKind.Base ? CanGrowOn(current)
                : block.Kind == BlockKind.Root ? CanRootThrough(current)
                : CanReplace(current) || block.Kind == BlockKind.Trunk && CanGrowThrough(current);
            if (!allowed) return null;
        }

        return placements;
    }

    bool CanReplace(BlockPermutation block) => replaceRules is { } rules
        ? Matches(rules, block)
        : BlockPalette.Replaceable(block) || block.Identifier.EndsWith("_leaves", StringComparison.Ordinal);

    bool CanGrowThrough(BlockPermutation block) => block.Identifier.EndsWith("_sapling", StringComparison.Ordinal) ||
        block.Identifier is "minecraft:azalea" or "minecraft:flowering_azalea" or "minecraft:mangrove_propagule" ||
        growThroughRules is { } rules && Matches(rules, block);

    bool CanGrowOn(BlockPermutation block) => groundRules is not { } rules || Matches(rules, block);

    bool CanRootThrough(BlockPermutation block) => rootRules is { } rules && Matches(rules, block);

    static bool Matches(BlockRule[] rules, BlockPermutation block) {
        foreach (BlockRule rule in rules) {
            if (rule.Matches(block)) return true;
        }

        return false;
    }

    BlockPermutation SelectLeaf() {
        if (leafBlocks.Length == 1) return leafBlocks[0].Block;

        int selected = random.Next(leafWeight);
        foreach ((BlockPermutation block, int weight) in leafBlocks) {
            selected -= weight;
            if (selected < 0) return block;
        }
        throw new InvalidDataException("The tree leaf weights are invalid.");
    }

    static BlockRule[]? ReadRules(JsonElement source, string name) {
        if (!source.TryGetProperty(name, out JsonElement entries)) return null;
        BlockRule[] rules = new BlockRule[entries.GetArrayLength()];
        for (int index = 0; index < rules.Length; index++) {
            JsonElement entry = entries[index];
            if (entry.ValueKind == JsonValueKind.String) {
                rules[index] = new BlockRule(entry.GetString(), default, []);
                continue;
            }
            string? identifier = entry.TryGetProperty("name", out JsonElement id) ? id.GetString() : null;
            FeatureExpression expression = entry.TryGetProperty("tags", out JsonElement tags) ? FeatureExpression.Parse(tags.GetString()!) : default;
            KeyValuePair<string, object>[] states = entry.TryGetProperty("states", out JsonElement state)
                ? state.EnumerateObject().Select(property => new KeyValuePair<string, object>(property.Name, StateValue(property.Value))).ToArray()
                : [];
            rules[index] = new BlockRule(identifier, expression, states);
        }

        return rules;
    }

    readonly record struct BlockRule(string? Identifier, FeatureExpression Tags, KeyValuePair<string, object>[] States) {
        internal bool Matches(BlockPermutation block) {
            if (Identifier is null) {
                BlockType? type = BlockPalette.FindType(block.Identifier);
                return Tags.Valid && type is not null && Tags.MatchesTags(type.Tags);
            }
            if (block.Identifier != Identifier) return false;
            foreach ((string name, object expected) in States) {
                if (!block.State.Values.TryGetValue(name, out object? actual) || !Equals(expected, actual)) return false;
            }

            return true;
        }
    }

    static BlockPermutation? Resolve(JsonElement value) {
        string? identifier = value.ValueKind == JsonValueKind.String ? value.GetString()
            : value.ValueKind == JsonValueKind.Object && value.TryGetProperty("name", out JsonElement name) ? name.GetString() : null;
        if (identifier is null || BlockPalette.FindDefaultPermutation(identifier) is not { } permutation) return null;
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("states", out JsonElement states)) return permutation;

        BlockState state = new() { Values = new(permutation.State.Values, StringComparer.Ordinal) };
        foreach (JsonProperty property in states.EnumerateObject()) {
            state.Values[property.Name] = StateValue(property.Value);
        }

        return BlockPalette.Find(identifier, state);
    }

    int Sample(JsonElement value, int fallback) {
        if (value.ValueKind == JsonValueKind.Number) return value.GetInt32();
        if (value.ValueKind != JsonValueKind.Object) return fallback;
        if (value.TryGetProperty("range_min", out JsonElement minimum) && value.TryGetProperty("range_max", out JsonElement maximum))
            return random.Next(minimum.GetInt32(), checked(maximum.GetInt32() + 1));

        int result = Number(value, "base", fallback);
        if (value.TryGetProperty("intervals", out JsonElement intervals)) {
            foreach (JsonElement interval in intervals.EnumerateArray()) result = checked(result + random.Next(interval.GetInt32()));
        }
        if (value.TryGetProperty("variance", out JsonElement variance)) result = checked(result + random.Next(variance.GetInt32()));
        if (value.TryGetProperty("height_rand_a", out JsonElement first)) result = checked(result + random.Next(first.GetInt32() + 1));
        if (value.TryGetProperty("height_rand_b", out JsonElement second))
            result = checked(result + random.Next(second.GetInt32() + 1));
        return result;
    }

    static int Number(JsonElement value, string name, int fallback) => value.TryGetProperty(name, out JsonElement number) ? number.GetInt32() : fallback;

    static object StateValue(JsonElement value) => value.ValueKind switch {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => value.GetInt32(),
        _ => throw new InvalidDataException("Unsupported tree block state value.")
    };

    (int X, int Z) HorizontalDirection() {
        (int dx, _, int dz) = BlockDirections.OffsetByFace[random.Next(2, 6)];
        return (dx, dz);
    }

    internal readonly record struct BlockPlacement(BlockPermutation Permutation, BlockKind Kind);
    internal enum BlockKind : byte { Base, Leaf, Trunk, Root }
    readonly record struct FoliageAttachment(int X, int Y, int Z, int RadiusOffset, bool DoubleTrunk);
}
