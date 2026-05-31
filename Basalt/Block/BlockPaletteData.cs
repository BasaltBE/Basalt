namespace Basalt.Server.Block;

using System.Text.Json.Serialization;


public sealed class BlockTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("states")]
    public List<string> States { get; set; } = [];
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

public sealed class BlockMetadataData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("hardness")]
    public float Hardness { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<BlockTypeData>))]
[JsonSerializable(typeof(List<BlockPermutationData>))]
[JsonSerializable(typeof(List<BlockMetadataData>))]
internal partial class BlockPaletteJsonContext : JsonSerializerContext
{
}







