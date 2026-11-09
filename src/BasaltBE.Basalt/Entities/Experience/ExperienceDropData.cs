using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Experience;

internal sealed class ExperienceDropTableData {
    [JsonInclude, JsonPropertyName("block")]
    public ExperienceRange[] Blocks = [];

    [JsonInclude, JsonPropertyName("entity")]
    public ExperienceRange[] Entities = [];

    [JsonInclude, JsonPropertyName("smelt")]
    public SmeltExperience[] Smelt = [];
}

internal sealed class ExperienceRange {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("minimum")]
    public int Minimum;

    [JsonInclude, JsonPropertyName("maximum")]
    public int Maximum;
}

internal sealed class SmeltExperience {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("experience")]
    public float Experience;
}

[JsonSerializable(typeof(ExperienceDropTableData))]
internal partial class ExperienceDropJsonContext : JsonSerializerContext {
}
