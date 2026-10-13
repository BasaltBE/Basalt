using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityFilter {
    [JsonInclude, JsonPropertyName("AND")]
    public EntityFilter[]? And;

    [JsonInclude, JsonPropertyName("OR")]
    public EntityFilter[]? Or;

    [JsonInclude, JsonPropertyName("all_of")]
    public EntityFilter[]? AllOf;

    [JsonInclude, JsonPropertyName("any_of")]
    public EntityFilter[]? AnyOf;

    [JsonInclude, JsonPropertyName("operator")]
    public EntityFilterOperand? Operator;

    [JsonInclude, JsonPropertyName("subject")]
    public EntityFilterOperand? Subject;

    [JsonInclude, JsonPropertyName("test")]
    public string? Test;

    [JsonInclude, JsonPropertyName("value")]
    public EntityFilterValue? Value;
}
