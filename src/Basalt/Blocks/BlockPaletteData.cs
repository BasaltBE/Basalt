namespace Basalt.Core.Blocks;

using System.Text.Json;
using System.Text.Json.Serialization;


public sealed class BlockTypeData {
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

public sealed class BlockPermutationData {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("hash")]
    public int Hash { get; set; }

    [JsonPropertyName("state")]
    public Dictionary<string, object> State { get; set; } = [];
}

public sealed class BlockDropData : Dictionary<string, BlockDropToolData>;

public sealed class BlockDropToolData : Dictionary<string, List<BlockDropEntryData>>;

public sealed class BlockDropEntryData {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("minAmount")]
    public int MinAmount { get; set; } = 1;

    [JsonPropertyName("maxAmount")]
    public int MaxAmount { get; set; } = 1;

    [JsonIgnore]
    public int Min => MinAmount;

    [JsonIgnore]
    public int Max => MaxAmount;

    [JsonPropertyName("chance")]
    public float Chance { get; set; } = 1.0f;
}

public sealed class BlockStateData {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<JsonElement> Values { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<BlockTypeData>))]
[JsonSerializable(typeof(List<BlockPermutationData>))]
[JsonSerializable(typeof(List<BlockDropData>))]
[JsonSerializable(typeof(Dictionary<string, BlockDropData>))]
[JsonSerializable(typeof(List<BlockStateData>))]
internal sealed partial class BlockPaletteJsonContext : JsonSerializerContext {
}







