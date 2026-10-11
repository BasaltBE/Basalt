using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Blocks;

public static class BlockPalette {
    public static readonly BlockPermutation[] Permutations = Load<BlockPermutation[]>("block_permutations.json");
    public static readonly BlockType[] Types = Load<BlockType[]>("block-types.json");
    static readonly Dictionary<string, BlockType> ByIdentifier = Types.ToDictionary(type => type.Identifier);
    static readonly Dictionary<string, BlockPermutation> DefaultPermutations = Permutations
        .GroupBy(permutation => permutation.Identifier)
        .ToDictionary(group => group.Key, group => group.First());
    static readonly Dictionary<(string Identifier, int StateHash), List<BlockPermutation>> ByState = BuildStateIndex();

    public static BlockType? FindType(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static BlockPermutation? FindDefaultPermutation(string identifier) => DefaultPermutations.GetValueOrDefault(identifier);

    public static BlockPermutation? Find(string identifier, BlockState state) {
        if (!ByState.TryGetValue((identifier, StateHash(state)), out List<BlockPermutation>? permutations)) {
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
        if (!ByState.TryGetValue((identifier, StateHash(state, name, value)), out List<BlockPermutation>? permutations)) {
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
        if (!ByState.TryGetValue((identifier, StateHash(state, changes)), out List<BlockPermutation>? permutations)) {
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

    static Dictionary<(string Identifier, int StateHash), List<BlockPermutation>> BuildStateIndex() {
        Dictionary<(string Identifier, int StateHash), List<BlockPermutation>> index = [];
        foreach (BlockPermutation permutation in Permutations) {
            (string Identifier, int StateHash) key = (permutation.Identifier, StateHash(permutation.State));
            if (!index.TryGetValue(key, out List<BlockPermutation>? permutations)) {
                permutations = [];
                index.Add(key, permutations);
            }
            permutations.Add(permutation);
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
            } else if (!state.Values.TryGetValue(candidateName, out expectedValue)) {
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
            } else if (!state.Values.TryGetValue(candidateName, out expectedValue)) {
                return false;
            }
            if (!Equals(candidateValue, expectedValue)) {
                return false;
            }
        }
        return true;
    }
}
