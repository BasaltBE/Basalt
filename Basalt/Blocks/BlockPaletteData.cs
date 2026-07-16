namespace Basalt.Core.Blocks;

using System.Text.Json;
using System.Text.Json.Serialization;


public sealed class BlockTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public Dictionary<string, JsonElement> Components { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("states")]
    public List<string> States { get; set; } = [];

    [JsonPropertyName("air")]
    public bool Air { get; set; }

    [JsonPropertyName("liquid")]
    public bool Liquid { get; set; }

    [JsonPropertyName("solid")]
    public bool Solid { get; set; }

    [JsonPropertyName("blastResistance")]
    public float BlastResistance { get; set; }

    [JsonPropertyName("brightness")]
    public float Brightness { get; set; }

    [JsonPropertyName("flameEncouragement")]
    public float FlameEncouragement { get; set; }

    [JsonPropertyName("flammability")]
    public float Flammability { get; set; }

    [JsonPropertyName("friction")]
    public float Friction { get; set; }

    [JsonPropertyName("hardness")]
    public float Hardness { get; set; }

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; }

    [JsonPropertyName("loggable")]
    public bool Loggable { get; set; }

    [JsonPropertyName("mapColor")]
    public string? MapColor { get; set; }
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

public sealed class BlockDropData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("drops")]
    public List<BlockDropEntryData> Drops { get; set; } = [];
}

public sealed class BlockDropEntryData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public int Min { get; set; } = 1;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 1;

    [JsonPropertyName("chance")]
    public float Chance { get; set; } = 1.0f;
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<BlockTypeData>))]
[JsonSerializable(typeof(List<BlockPermutationData>))]
[JsonSerializable(typeof(List<BlockDropData>))]
internal sealed partial class BlockPaletteJsonContext : JsonSerializerContext
{
}







