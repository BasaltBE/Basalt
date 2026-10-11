using System.Reflection;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Blocks;

public static class BlockPalette {
    public static readonly BlockPermutation[] Permutations = Load<BlockPermutation[]>("block_permutations.json");
    public static readonly BlockType[] Types = Load<BlockType[]>("block-types.json");
    static readonly Dictionary<(string Identifier, string State), BlockPermutation> ByState = Permutations.ToDictionary(
        permutation => (permutation.Identifier, StateKey(permutation.State)));

    public static BlockPermutation? Find(string identifier, BlockState state) =>
        ByState.GetValueOrDefault((identifier, StateKey(state)));

    static T Load<T>(string fileName) where T : class {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream($"BasaltBE.Data.Data.{fileName}")
            ?? throw new InvalidOperationException($"Block data file '{fileName}' is missing.");

        return JsonSerializer.Deserialize<T>(stream)
            ?? throw new JsonException($"Block data file '{fileName}' is empty.");
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
