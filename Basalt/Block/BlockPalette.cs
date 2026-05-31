namespace Basalt.Server.Block;

using Basalt.Server.Block.Types;
using Basalt.Server.Block.Traits;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;


public sealed class BlockPalette
{
    private const string AirIdentifier = "minecraft:air";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();

    [ModuleInitializer]
    public static void Initialize()
    {
        LoadVanilla();
        BlockTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
    }

    public IReadOnlyDictionary<string, BlockType> Types => BlockType.Types;
    public IReadOnlyDictionary<int, BlockPermutation> Permutations => BlockPermutation.Permutations;

    public List<BlockType> GetAllTypes()
    {
        return [.. Types.Values];
    }

    public List<BlockPermutation> GetAllPermutations()
    {
        return [.. Permutations.Values];
    }

    public BlockType ResolveType(BlockIdentifier identifier)
    {
        return ResolveType(identifier.ToIdentifier());
    }

    public BlockType ResolveType(string identifier)
    {
        return BlockType.GetOrAir(identifier);
    }

    public BlockPermutation ResolvePermutation(BlockIdentifier identifier, BlockState? state = null)
    {
        return ResolvePermutation(identifier.ToIdentifier(), state);
    }

    public BlockPermutation ResolvePermutation(string identifier, BlockState? state = null)
    {
        BlockType type = ResolveType(identifier);
        return type.GetPermutation(state);
    }

    public BlockPermutation ResolvePermutation(int networkId, BlockState? state = null)
    {
        if (BlockPermutation.Permutations.TryGetValue(networkId, out BlockPermutation? permutation))
        {
            return state is null ? permutation : permutation.Type.GetPermutation(state);
        }

        return ResolvePermutation(AirIdentifier, state);
    }

    public BlockPalette RegisterType(params BlockType[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            BlockType type = types[i];
            for (int j = 0; j < type.Permutations.Count; j++)
            {
                RegisterPermutation(type.Permutations[j]);
            }
        }

        return this;
    }

    public bool RegisterPermutation(BlockPermutation permutation)
    {
        if (BlockPermutation.Permutations.ContainsKey(permutation.NetworkId))
        {
            return false;
        }

        BlockPermutation.Permutations[permutation.NetworkId] = permutation;
        return true;
    }

    public static void LoadVanilla(string? dataDirectory = null)
    {
        if (_vanillaLoaded)
        {
            return;
        }

        lock (LoadLock)
        {
            // TODO: Make this multi threaded when multi threading is in place.
            if (_vanillaLoaded)
            {
                return;
            }

            string root = ResolveDataDirectory(dataDirectory);
            string typesPath = Path.Combine(root, "block_types.json");
            string permutationsPath = Path.Combine(root, "block_permutations.json");
            string metadataPath = Path.Combine(root, "block_metadata.json");
            List<BlockTypeData> types = ReadTypes(typesPath);
            List<BlockPermutationData> permutations = ReadPermutations(permutationsPath);
            List<BlockMetadataData> metadata = ReadMetadata(metadataPath);
            LoadRegistries(types, permutations, metadata);

            _vanillaLoaded = true;
        }
    }

    private static List<BlockTypeData> ReadTypes(string typesPath)
    {
        using FileStream stream = File.OpenRead(typesPath);
        List<BlockTypeData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockTypeData);
        return result ?? [];
    }

    private static List<BlockPermutationData> ReadPermutations(string permutationsPath)
    {
        using FileStream stream = File.OpenRead(permutationsPath);
        List<BlockPermutationData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockPermutationData);
        return result ?? [];
    }

    private static List<BlockMetadataData> ReadMetadata(string metadataPath)
    {
        if (!File.Exists(metadataPath)) return [];
        using FileStream stream = File.OpenRead(metadataPath);
        List<BlockMetadataData>? result = JsonSerializer.Deserialize(stream, BlockPaletteJsonContext.Default.ListBlockMetadataData);
        return result ?? [];
    }

    private static void LoadRegistries(List<BlockTypeData> types, List<BlockPermutationData> permutations, List<BlockMetadataData> metadata)
    {
        BlockType.EnsureRegistryCapacity(types.Count + 1);
        BlockPermutation.EnsureRegistryCapacity(permutations.Count);

        for (int i = 0; i < types.Count; i++)
        {
            string identifier = types[i].Identifier;
            if (string.IsNullOrEmpty(identifier))
            {
                continue;
            }

            BlockType type = BlockType.Get(identifier) ?? new BlockType(identifier);

            for (int j = 0; j < types[i].Components.Count; j++)
            {
                type.EnsureComponent(types[i].Components[j]);
            }

            for (int j = 0; j < types[i].Tags.Count; j++)
            {
                type.EnsureTag(types[i].Tags[j]);
            }

            for (int j = 0; j < types[i].States.Count; j++)
            {
                type.EnsureState(types[i].States[j]);
            }
        }

        _ = BlockType.Get(AirIdentifier) ?? new BlockType(AirIdentifier);

        for (int i = 0; i < metadata.Count; i++)
        {
            if (BlockType.Types.TryGetValue(metadata[i].Identifier, out BlockType? type))
            {
                type.Hardness = metadata[i].Hardness;
            }
        }

        for (int i = 0; i < permutations.Count; i++)
        {
            BlockPermutationData entry = permutations[i];
            if (string.IsNullOrEmpty(entry.Identifier) || BlockPermutation.Permutations.ContainsKey(entry.Hash))
            {
                continue;
            }

            BlockType type = BlockType.GetOrAir(entry.Identifier);
            BlockState state = ParseState(entry.State);
            BlockPermutation permutation = new(entry.Hash, state, type);
            BlockPermutation.Permutations[entry.Hash] = permutation;
            type.RegisterPermutation(permutation);
        }
    }

    private static BlockState ParseState(Dictionary<string, object> source)
    {
        BlockState state = [];
        foreach ((string key, object value) in source)
        {
            state[key] = ToStateValue(key, value);
        }

        return state;
    }

    private static BlockStateValue ToStateValue(string key, object raw)
    {
        if (raw is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out long number)
                    ? number
                    : throw new InvalidOperationException($"Invalid numeric state value for '{key}'."),
                _ => throw new InvalidOperationException($"Unsupported state value kind '{element.ValueKind}' for '{key}'.")
            };
        }

        return raw switch
        {
            bool flag => flag,
            string text => text,
            byte number => number,
            short number => number,
            int number => number,
            long number => number,
            _ => throw new InvalidOperationException($"Unsupported state value type '{raw.GetType().Name}' for '{key}'.")
        };
    }


    private static string ResolveDataDirectory(string? overrideDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return overrideDirectory;
        }

        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, "Protocol", "Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Protocol/Data directory.");
    }

}







