using System.Text.Json.Serialization;

namespace Basalt.Block;

public sealed class BlockTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;
}

public sealed class BlockPermutationData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("hash")]
    public int Hash { get; set; }

    [JsonPropertyName("state")]
    public Dictionary<string, object> State { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<BlockTypeData>))]
[JsonSerializable(typeof(List<BlockPermutationData>))]
internal partial class BlockPaletteJsonContext : JsonSerializerContext
{
}
