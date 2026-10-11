using System.Reflection;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Blocks;

public static class BlockPalette {
    public static readonly BlockPermutation[] Permutations = Load();
    static readonly Dictionary<(string Identifier, string State), BlockPermutation> ByState = Permutations.ToDictionary(
        permutation => (permutation.Identifier, StateKey(permutation.State)));

    public static BlockPermutation? Find(string identifier, BlockState state) =>
        ByState.GetValueOrDefault((identifier, StateKey(state)));

    static BlockPermutation[] Load() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.block_permutations.json")
            ?? throw new InvalidOperationException("Block permutation data is missing.");

        return JsonSerializer.Deserialize<BlockPermutation[]>(stream)
            ?? throw new JsonException("Block permutation data is empty.");
    }

    static string StateKey(BlockState state) {
        StringBuilder key = new();
        foreach ((string name, object value) in state.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal)) {
            string serialized = JsonSerializer.Serialize(value);
            key.Append(name.Length).Append(':').Append(name).Append(serialized.Length).Append(':').Append(serialized);
        }
        return key.ToString();
    }
}
