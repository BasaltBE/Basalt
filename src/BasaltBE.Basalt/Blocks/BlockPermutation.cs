using System.Text.Json.Serialization;
using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Blocks;

public sealed class BlockPermutation {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("hash")]
    public int Hash;

    [JsonInclude, JsonPropertyName("state")]
    public BlockState State = new();
}
