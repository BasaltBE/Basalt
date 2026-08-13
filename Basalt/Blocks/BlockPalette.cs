namespace Basalt.Core.Blocks;

using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Components;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BedrockProtocol.Types;

public sealed class BlockPalette {
    private const string AirIdentifier = "minecraft:air";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();

#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
#pragma warning restore CA2255
    {
#pragma warning disable IL2026
        LoadVanilla();
        BlockTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
#pragma warning restore IL2026
    }

    public static IReadOnlyDictionary<string, BlockType> Types => BlockType.Types;
    public static IReadOnlyDictionary<int, BlockPermutation> Permutations => BlockPermutation.Permutations;

    public static List<BlockType> GetAllTypes() {
        return [.. Types.Values];
    }

    public static List<BlockPermutation> GetAllPermutations() {
        return [.. Permutations.Values];
    }

    public static List<ServerBlockProperty> GetCustomBlockEntries() {
        return CustomBlockType.GetEntries();
    }

    public static BlockType ResolveType(BlockIdentifier identifier) {
        return ResolveType(identifier.ToIdentifier());
    }

    public static BlockType ResolveType(string identifier) {
        return BlockType.GetOrAir(identifier);
    }

    public static BlockPermutation ResolvePermutation(BlockIdentifier identifier, BlockState? state = null) {
        return ResolvePermutation(identifier.ToIdentifier(), state);
    }

    public static BlockPermutation ResolvePermutation(string identifier, BlockState? state = null) {
        BlockType type = ResolveType(identifier);
        return type.GetPermutation(state);
    }

    public static BlockPermutation ResolvePermutation(int networkId, BlockState? state = null) {
        if (BlockPermutation.Permutations.TryGetValue(networkId, out BlockPermutation? permutation)) {
            return state is null ? permutation : permutation.Type.GetPermutation(state);
        }

        return ResolvePermutation(AirIdentifier, state);
    }

    public BlockPalette RegisterType(params BlockType[] types) {
        for (int i = 0; i < types.Length; i++) {
            BlockType type = types[i];
            for (int j = 0; j < type.Permutations.Count; j++) {
                RegisterPermutation(type.Permutations[j]);
            }
        }

        return this;
    }

    public static bool RegisterPermutation(BlockPermutation permutation) {
        if (BlockPermutation.Permutations.ContainsKey(permutation.NetworkId)) {
            return false;
        }

        BlockPermutation.Permutations[permutation.NetworkId] = permutation;
        return true;
    }

    public static void LoadVanilla(string? dataDirectory = null) {
        if (_vanillaLoaded) {
            return;
        }

        lock (LoadLock) {
            // TODO: Make this multi threaded when multi threading is in place.
            if (_vanillaLoaded) {
                return;
            }

            if (!string.IsNullOrWhiteSpace(dataDirectory)) {
                string typesPath = Path.Combine(dataDirectory, "block_types.json");
                string permutationsPath = Path.Combine(dataDirectory, "block_permutations.json");
                string dropsPath = Path.Combine(dataDirectory, "block_drops.json");
                List<BlockTypeData> types = ReadTypesFromFile(typesPath);
                List<BlockPermutationData> permutations = ReadPermutationsFromFile(permutationsPath);
                List<BlockDropData> drops = ReadDropsFromFile(dropsPath);
                LoadRegistries(types, permutations, drops);
            }
            else {
                List<BlockTypeData> types = ReadTypes("block_types.json");
                List<BlockPermutationData> permutations = ReadPermutations("block_permutations.json");
                List<BlockDropData> drops = ReadDrops("block_drops.json");
                LoadRegistries(types, permutations, drops);
            }

            _vanillaLoaded = true;
        }
    }

    private static List<BlockTypeData> ReadTypes(string resourceName) {
        using Stream stream = ProtocolData.Require(resourceName);
        List<BlockTypeData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockTypeData);
        return result ?? [];
    }

    private static List<BlockPermutationData> ReadPermutations(string resourceName) {
        using Stream stream = ProtocolData.Require(resourceName);
        List<BlockPermutationData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockPermutationData);
        return result ?? [];
    }

    private static List<BlockDropData> ReadDrops(string resourceName) {
        Stream? stream = ProtocolData.Open(resourceName);
        if (stream is null) return [];
        using (stream) {
            List<BlockDropData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockDropData);
            return result ?? [];
        }
    }

    private static List<BlockTypeData> ReadTypesFromFile(string typesPath) {
        using FileStream stream = File.OpenRead(typesPath);
        List<BlockTypeData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockTypeData);
        return result ?? [];
    }

    private static List<BlockPermutationData> ReadPermutationsFromFile(string permutationsPath) {
        using FileStream stream = File.OpenRead(permutationsPath);
        List<BlockPermutationData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockPermutationData);
        return result ?? [];
    }

    private static List<BlockDropData> ReadDropsFromFile(string dropsPath) {
        if (!File.Exists(dropsPath)) {
            return [];
        }

        using FileStream stream = File.OpenRead(dropsPath);
        List<BlockDropData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockDropData);
        return result ?? [];
    }

    private static void LoadRegistries(List<BlockTypeData> types, List<BlockPermutationData> permutations, List<BlockDropData> drops) {
        BlockType.EnsureRegistryCapacity(types.Count + 1);
        BlockPermutation.EnsureRegistryCapacity(permutations.Count);

        for (int i = 0; i < types.Count; i++) {
            string identifier = types[i].Identifier;
            if (string.IsNullOrEmpty(identifier)) {
                continue;
            }


            // Vanilla Has them as non solids
            bool Solid = types[i].Solid;
            if (
                types[i].Tags.Contains("minecraft:cornerable_stairs")
                || types[i].States.Contains("powered_shelf_type")
                || types[i].Identifier.Contains("_shulker_box")
                || types[i].Identifier.Contains("_stained_glass")
                || types[i].Identifier.Contains("minecraft:grass_path")
                ) {
                Solid = true;
            }


            BlockType type = BlockType.Get(identifier) ?? new BlockType(identifier);
            type.Air = types[i].Air;
            type.Liquid = types[i].Liquid;
            type.Solid = Solid;
            type.BlastResistance = types[i].BlastResistance;
            type.Brightness = types[i].Brightness;
            type.FlameEncouragement = types[i].FlameEncouragement;
            type.Flammability = types[i].Flammability;
            type.Friction = types[i].Friction;
            type.Hardness = types[i].Hardness;
            type.Opacity = types[i].Opacity;
            type.Loggable = types[i].Loggable;
            type.MapColor = types[i].MapColor;

            foreach (KeyValuePair<string, JsonElement> component in types[i].Components) {
                BlockComponent? blockComponent = BlockComponentParser.Parse(component.Key, component.Value);
                if (blockComponent is not null) {
                    type.AddComponent(blockComponent);
                }
                else {
                    type.EnsureComponent(component.Key);
                }
            }

            for (int j = 0; j < types[i].Tags.Count; j++) {
                type.EnsureTag(types[i].Tags[j]);
            }

            for (int j = 0; j < types[i].States.Count; j++) {
                type.EnsureState(types[i].States[j]);
            }
        }

        _ = BlockType.Get(AirIdentifier) ?? new BlockType(AirIdentifier);

        for (int i = 0; i < permutations.Count; i++) {
            BlockPermutationData entry = permutations[i];
            if (string.IsNullOrEmpty(entry.Identifier) || BlockPermutation.Permutations.ContainsKey(entry.Hash)) {
                continue;
            }

            BlockType type = BlockType.GetOrAir(entry.Identifier);
            BlockState state = ParseState(entry.State);
            BlockPermutation permutation = new(entry.Hash, state, type);
            BlockPermutation.Permutations[entry.Hash] = permutation;
            type.RegisterPermutation(permutation);
        }

        BlockDropRegistry.Load(drops);
    }

    private static BlockState ParseState(Dictionary<string, object> source) {
        BlockState state = [];
        foreach ((string key, object value) in source) {
            state[key] = ToStateValue(key, value);
        }

        return state;
    }

    private static BlockStateValue ToStateValue(string key, object raw) {
        if (raw is JsonElement element) {
            return element.ValueKind switch {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out long number)
                    ? number
                    : throw new InvalidOperationException($"Invalid numeric state value for '{key}'."),
                _ => throw new InvalidOperationException($"Unsupported state value kind '{element.ValueKind}' for '{key}'.")
            };
        }

        return raw switch {
            bool flag => flag,
            string text => text,
            byte number => number,
            short number => number,
            int number => number,
            long number => number,
            _ => throw new InvalidOperationException($"Unsupported state value type '{raw.GetType().Name}' for '{key}'.")
        };
    }


}







