using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks;

public static class BlockPalette {
    public static readonly BlockPermutation[] Permutations = LoadPermutations();
    public static readonly BlockType[] Types = LoadBlockTypes();
    static readonly Dictionary<string, BlockType> ByIdentifier = Types.ToDictionary(type => type.Identifier);
    static readonly Dictionary<string, BlockPermutation> DefaultPermutations = BuildDefaultPermutations();
    static readonly Dictionary<int, BlockPermutation> ByNetworkId = Permutations.ToDictionary(permutation => permutation.NetworkId);
    static readonly Dictionary<(string Identifier, int StateHash), BlockPermutation[]> ByState = BuildStateIndex();
    static readonly Dictionary<(string Identifier, string Half), BlockPermutation> DoubleSlabs = BuildDoubleSlabs();
    static readonly Dictionary<string, string> StrippedBlocks = BuildStrippedBlocks();
    static readonly string[] ConnectionStates = [
        "minecraft:connection_east",
        "minecraft:connection_north",
        "minecraft:connection_south",
        "minecraft:connection_west"
    ];
    static readonly HashSet<string> PaneIdentifiers = BuildIdentifiers(ConnectionStates, "_pane");
    static readonly HashSet<string> BarIdentifiers = BuildIdentifiers(ConnectionStates, "_bars");
    static readonly HashSet<string> GlassIdentifiers = BuildIdentifiers([], "glass");
    static readonly HashSet<string> PaneAndBarIdentifiers = [.. PaneIdentifiers, .. BarIdentifiers];
    static readonly HashSet<string> FenceIdentifiers = BuildIdentifiers(
        ConnectionStates,
        "fence"
    );
    static readonly HashSet<string> FenceGateIdentifiers = BuildIdentifiers(
        ["in_wall_bit", "minecraft:cardinal_direction", "open_bit"],
        "fence_gate"
    );
    static readonly HashSet<string> WallIdentifiers = BuildIdentifiers(
        ["wall_connection_type_east", "wall_connection_type_north", "wall_connection_type_south", "wall_connection_type_west"]
    );
    static readonly Dictionary<(string Identifier, byte Connections), BlockPermutation> FencePermutations = BuildConnectionPermutations(FenceIdentifiers);
    static readonly Dictionary<(string Identifier, byte Connections), BlockPermutation> PanePermutations = BuildConnectionPermutations(PaneAndBarIdentifiers);
    static readonly Dictionary<(string Identifier, string Direction, bool Open, bool InWall), BlockPermutation> FenceGatePermutations = BuildFenceGatePermutations();
    static readonly Dictionary<string, BlockTrait[]> TraitsByIdentifier = new(StringComparer.Ordinal);
    static readonly HashSet<Type> RegisteredTraits = [];

    public static BlockType? FindType(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static BlockPermutation? FindDefaultPermutation(string identifier) => DefaultPermutations.GetValueOrDefault(identifier);

    public static BlockPermutation? FindByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    internal static BlockPermutation? FindDoubleSlab(string identifier, string half) =>
        DoubleSlabs.GetValueOrDefault((identifier, half));

    internal static bool IsSlab(string identifier) => DoubleSlabs.ContainsKey((identifier, "bottom"));

    internal static bool IsCornerableStairs(string identifier) =>
        ByIdentifier.TryGetValue(identifier, out BlockType? type) &&
        Array.IndexOf(type.Tags, "minecraft:cornerable_stairs") >= 0;

    internal static bool IsFence(string identifier) => FenceIdentifiers.Contains(identifier);

    internal static bool IsFenceGate(string identifier) => FenceGateIdentifiers.Contains(identifier);

    internal static bool IsWall(string identifier) => WallIdentifiers.Contains(identifier);

    internal static bool PaneConnects(string neighborIdentifier) {
        if (PaneAndBarIdentifiers.Contains(neighborIdentifier) || GlassIdentifiers.Contains(neighborIdentifier))
            return true;

        return ByIdentifier.TryGetValue(neighborIdentifier, out BlockType? neighbor) && neighbor.Solid && neighbor.Opacity >= 15;
    }

    internal static BlockPermutation? FindStripped(BlockPermutation block) =>
        StrippedBlocks.TryGetValue(block.Identifier, out string? strippedIdentifier)
            ? Find(strippedIdentifier, block.State) ?? FindDefaultPermutation(strippedIdentifier)
            : null;

    internal static BlockPermutation? FindFencePermutation(string identifier, byte connections) =>
        FencePermutations.GetValueOrDefault((identifier, connections));

    internal static BlockPermutation? FindPanePermutation(string identifier, byte connections) =>
        PanePermutations.GetValueOrDefault((identifier, connections));

    internal static BlockPermutation? FindFenceGatePermutation(string identifier, string direction, bool open, bool inWall) =>
        FenceGatePermutations.GetValueOrDefault((identifier, direction, open, inWall));

    internal static bool FenceConnects(string fenceIdentifier, string neighborIdentifier) {
        if (IsFence(neighborIdentifier)) {
            bool fenceIsWood = Array.IndexOf(ByIdentifier[fenceIdentifier].Tags, "wood") >= 0;
            bool neighborIsWood = Array.IndexOf(ByIdentifier[neighborIdentifier].Tags, "wood") >= 0;
            return fenceIsWood == neighborIsWood;
        }

        if (IsFenceGate(neighborIdentifier))
            return Array.IndexOf(ByIdentifier[fenceIdentifier].Tags, "wood") >= 0;

        if (IsWall(neighborIdentifier))
            return true;

        return ByIdentifier.TryGetValue(neighborIdentifier, out BlockType? neighbor) && neighbor.Solid && neighbor.Opacity >= 15;
    }

    /// <summary>Registers one shared trait instance for each block identifier declared by the trait.</summary>
    public static void RegisterTrait<TTrait>() where TTrait : BlockTrait, new() {
        Type traitType = typeof(TTrait);
        BlockTraitAppliesToAttribute targets = traitType.GetCustomAttribute<BlockTraitAppliesToAttribute>()
            ?? throw new InvalidOperationException($"Block trait '{traitType}' has no BlockTraitAppliesTo attribute.");
        RegisterTrait<TTrait>(targets.Identifiers);
    }

    public static void RegisterTrait<TTrait>(string blockTag) where TTrait : BlockTrait, new() {
        string[] identifiers = Types
            .Where(type => Array.IndexOf(type.Tags, blockTag) >= 0)
            .Select(type => type.Identifier)
            .ToArray();
        RegisterTrait<TTrait>(identifiers);
    }

    static void RegisterTrait<TTrait>(string[] identifiers) where TTrait : BlockTrait, new() {
        Type traitType = typeof(TTrait);
        if (RegisteredTraits.Contains(traitType))
            throw new InvalidOperationException($"Block trait '{traitType}' is already registered.");
        if (identifiers.Length == 0)
            throw new InvalidOperationException($"Block trait '{traitType}' has no block targets.");

        foreach (string identifier in identifiers) {
            if (!ByIdentifier.ContainsKey(identifier))
                throw new InvalidOperationException($"Block trait '{traitType}' targets unknown block '{identifier}'.");
        }

        TTrait trait = new();
        RegisteredTraits.Add(traitType);
        foreach (string identifier in identifiers) {
            BlockTrait[] current = TraitsByIdentifier.GetValueOrDefault(identifier) ?? [];
            BlockTrait[] updated = new BlockTrait[current.Length + 1];
            current.CopyTo(updated, 0);
            updated[^1] = trait;
            TraitsByIdentifier[identifier] = updated;
        }
    }

    internal static bool OnInteract(string identifier, Player player, BlockPos position) {
        if (!TraitsByIdentifier.TryGetValue(identifier, out BlockTrait[]? traits))
            return false;

        foreach (BlockTrait trait in traits) {
            if (trait.OnInteract(player, position))
                return true;
        }
        return false;
    }

    internal static bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        if (y < -64 || y >= 320 || dimension.GetChunk(x >> 4, z >> 4) is null)
            return false;

        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (!TraitsByIdentifier.TryGetValue(current.Identifier, out BlockTrait[]? traits))
            return false;

        bool updated = false;
        foreach (BlockTrait trait in traits)
            updated |= trait.OnNeighborUpdate(dimension, x, y, z);
        return updated;
    }

    public static BlockPermutation? Find(string identifier, BlockState state) {
        if (!ByState.TryGetValue((identifier, StateHash(state)), out BlockPermutation[]? permutations)) {
            return null;
        }

        foreach (BlockPermutation permutation in permutations) {
            if (StateMatches(permutation.State, state)) {
                return permutation;
            }
        }
        return null;
    }

    public static BlockPermutation? Find(string identifier, BlockState state, string name, object value) {
        if (!ByState.TryGetValue((identifier, StateHash(state, name, value)), out BlockPermutation[]? permutations)) {
            return null;
        }

        foreach (BlockPermutation permutation in permutations) {
            if (StateMatches(permutation.State, state, name, value)) {
                return permutation;
            }
        }
        return null;
    }

    public static BlockPermutation? Find(string identifier, BlockState state, BlockState changes) {
        if (!ByState.TryGetValue((identifier, StateHash(state, changes)), out BlockPermutation[]? permutations)) {
            return null;
        }

        foreach (BlockPermutation permutation in permutations) {
            if (StateMatches(permutation.State, state, changes)) {
                return permutation;
            }
        }
        return null;
    }

    static T Load<T>(string fileName) where T : class {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream($"BasaltBE.Data.Data.{fileName}")
            ?? throw new InvalidOperationException($"Block data file '{fileName}' is missing.");

        return JsonSerializer.Deserialize<T>(stream)
            ?? throw new JsonException($"Block data file '{fileName}' is empty.");
    }

    static BlockType[] LoadBlockTypes() {
        BlockType[] types = Load<BlockType[]>("block-types.json");
        foreach (BlockType type in types) {
            foreach (string tag in type.Tags) {
                type.ToolTypes |= tag switch {
                    "minecraft:is_pickaxe_item_destructible" => ToolType.Pickaxe,
                    "minecraft:is_axe_item_destructible" => ToolType.Axe,
                    "minecraft:is_shovel_item_destructible" => ToolType.Shovel,
                    "minecraft:is_hoe_item_destructible" => ToolType.Hoe,
                    _ => ToolType.None
                };
            }
        }
        return types;
    }

    static Dictionary<string, string> BuildStrippedBlocks() {
        Dictionary<string, string> stripped = new(StringComparer.Ordinal);
        foreach (BlockType type in Types) {
            int separator = type.Identifier.IndexOf(':');
            string strippedName = type.Identifier[(separator + 1)..];
            if (!strippedName.StartsWith("stripped_", StringComparison.Ordinal))
                continue;

            string originalIdentifier = type.Identifier[..(separator + 1)] + strippedName["stripped_".Length..];
            if (ByIdentifier.ContainsKey(originalIdentifier))
                stripped.TryAdd(originalIdentifier, type.Identifier);
        }
        return stripped;
    }

    static BlockPermutation[] LoadPermutations() {
        BlockPermutation[] permutations = Load<BlockPermutation[]>("block_permutations.json");
        foreach (BlockPermutation permutation in permutations) {
            permutation.NetworkId = permutation.Hash;
        }

        return permutations;
    }

    static Dictionary<string, BlockPermutation> BuildDefaultPermutations() {
        Dictionary<string, BlockPermutation> defaults = [];
        foreach (BlockPermutation permutation in Permutations) {
            defaults.TryAdd(permutation.Identifier, permutation);
        }
        return defaults;
    }

    static HashSet<string> BuildIdentifiers(string[] states, string? identifierSuffix = null) {
        HashSet<string> identifiers = new(StringComparer.Ordinal);
        foreach (BlockType type in Types) {
            if (identifierSuffix is not null && !type.Identifier.EndsWith(identifierSuffix, StringComparison.Ordinal))
                continue;

            bool matches = true;
            foreach (string state in states) {
                if (Array.IndexOf(type.States, state) >= 0)
                    continue;

                matches = false;
                break;
            }

            if (matches)
                identifiers.Add(type.Identifier);
        }

        return identifiers;
    }

    static Dictionary<(string Identifier, byte Connections), BlockPermutation> BuildConnectionPermutations(HashSet<string> identifiers) {
        Dictionary<(string Identifier, byte Connections), BlockPermutation> permutations = [];
        foreach (BlockPermutation permutation in Permutations) {
            if (!identifiers.Contains(permutation.Identifier))
                continue;

            byte connections = 0;
            if (permutation.State.Values.GetValueOrDefault("minecraft:connection_east") is true)
                connections |= 1;
            if (permutation.State.Values.GetValueOrDefault("minecraft:connection_north") is true)
                connections |= 2;
            if (permutation.State.Values.GetValueOrDefault("minecraft:connection_south") is true)
                connections |= 4;
            if (permutation.State.Values.GetValueOrDefault("minecraft:connection_west") is true)
                connections |= 8;
            permutations.TryAdd((permutation.Identifier, connections), permutation);
        }

        return permutations;
    }

    static Dictionary<(string Identifier, string Direction, bool Open, bool InWall), BlockPermutation> BuildFenceGatePermutations() {
        Dictionary<(string Identifier, string Direction, bool Open, bool InWall), BlockPermutation> permutations = [];
        foreach (BlockPermutation permutation in Permutations) {
            if (!FenceGateIdentifiers.Contains(permutation.Identifier) ||
                permutation.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string direction ||
                permutation.State.Values.GetValueOrDefault("open_bit") is not bool open ||
                permutation.State.Values.GetValueOrDefault("in_wall_bit") is not bool inWall)
                continue;

            permutations.TryAdd((permutation.Identifier, direction, open, inWall), permutation);
        }

        return permutations;
    }

    static Dictionary<(string Identifier, string Half), BlockPermutation> BuildDoubleSlabs() {
        Dictionary<(string Identifier, string Half), BlockPermutation> slabs = [];
        foreach (BlockPermutation permutation in Permutations) {
            int namespaceEnd = permutation.Identifier.IndexOf(':') + 1;
            int doubleToken = permutation.Identifier.IndexOf("double_", namespaceEnd, StringComparison.Ordinal);
            if (doubleToken < 0 ||
                !permutation.State.Values.TryGetValue("minecraft:vertical_half", out object? half) ||
                half is not string halfName)
                continue;

            string slabIdentifier = permutation.Identifier.Remove(doubleToken, "double_".Length);
            if (ByIdentifier.ContainsKey(slabIdentifier))
                slabs.TryAdd((slabIdentifier, halfName), permutation);
        }

        return slabs;
    }

    static Dictionary<(string Identifier, int StateHash), BlockPermutation[]> BuildStateIndex() {
        Dictionary<(string Identifier, int StateHash), BlockPermutation[]> index = new(Permutations.Length);
        foreach (BlockPermutation permutation in Permutations) {
            (string Identifier, int StateHash) key = (permutation.Identifier, StateHash(permutation.State));
            if (index.TryGetValue(key, out BlockPermutation[]? permutations)) {
                Array.Resize(ref permutations, permutations.Length + 1);
                permutations[^1] = permutation;
                index[key] = permutations;
            }
            else {
                index.Add(key, [permutation]);
            }
        }
        return index;
    }

    static int StateHash(BlockState state) {
        int hash = state.Values.Count;
        foreach ((string name, object value) in state.Values) {
            hash = unchecked(hash + HashCode.Combine(StringComparer.Ordinal.GetHashCode(name), value?.GetHashCode() ?? 0));
        }
        return hash;
    }

    static int StateHash(BlockState state, string name, object value) {
        bool containsName = state.Values.ContainsKey(name);
        int hash = containsName ? state.Values.Count : state.Values.Count + 1;
        foreach ((string stateName, object stateValue) in state.Values) {
            object valueToHash = stateName == name ? value : stateValue;
            hash = unchecked(hash + HashCode.Combine(StringComparer.Ordinal.GetHashCode(stateName), valueToHash?.GetHashCode() ?? 0));
        }
        if (!containsName) {
            hash = unchecked(hash + HashCode.Combine(StringComparer.Ordinal.GetHashCode(name), value?.GetHashCode() ?? 0));
        }
        return hash;
    }

    static int StateHash(BlockState state, BlockState changes) {
        int count = state.Values.Count;
        foreach (string name in changes.Values.Keys) {
            if (!state.Values.ContainsKey(name)) {
                count++;
            }
        }

        int hash = count;
        foreach ((string name, object value) in state.Values) {
            object valueToHash = changes.Values.TryGetValue(name, out object? changedValue) ? changedValue : value;
            hash = unchecked(hash + HashCode.Combine(StringComparer.Ordinal.GetHashCode(name), valueToHash?.GetHashCode() ?? 0));
        }
        foreach ((string name, object value) in changes.Values) {
            if (!state.Values.ContainsKey(name)) {
                hash = unchecked(hash + HashCode.Combine(StringComparer.Ordinal.GetHashCode(name), value?.GetHashCode() ?? 0));
            }
        }
        return hash;
    }

    static bool StateMatches(BlockState candidate, BlockState expected) {
        if (candidate.Values.Count != expected.Values.Count) {
            return false;
        }

        foreach ((string name, object value) in expected.Values) {
            if (!candidate.Values.TryGetValue(name, out object? candidateValue) || !Equals(candidateValue, value)) {
                return false;
            }
        }
        return true;
    }

    static bool StateMatches(BlockState candidate, BlockState state, string name, object value) {
        int expectedCount = state.Values.Count + (state.Values.ContainsKey(name) ? 0 : 1);
        if (candidate.Values.Count != expectedCount) {
            return false;
        }

        foreach ((string candidateName, object candidateValue) in candidate.Values) {
            object? expectedValue;
            if (candidateName == name) {
                expectedValue = value;
            }
            else if (!state.Values.TryGetValue(candidateName, out expectedValue)) {
                return false;
            }
            if (!Equals(candidateValue, expectedValue)) {
                return false;
            }
        }
        return true;
    }

    static bool StateMatches(BlockState candidate, BlockState state, BlockState changes) {
        int expectedCount = state.Values.Count;
        foreach (string name in changes.Values.Keys) {
            if (!state.Values.ContainsKey(name)) {
                expectedCount++;
            }
        }
        if (candidate.Values.Count != expectedCount) {
            return false;
        }

        foreach ((string candidateName, object candidateValue) in candidate.Values) {
            object? expectedValue;
            if (changes.Values.TryGetValue(candidateName, out object? changedValue)) {
                expectedValue = changedValue;
            }
            else if (!state.Values.TryGetValue(candidateName, out expectedValue)) {
                return false;
            }
            if (!Equals(candidateValue, expectedValue)) {
                return false;
            }
        }
        return true;
    }
}
