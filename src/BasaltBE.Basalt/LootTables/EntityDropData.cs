using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.LootTables;

internal sealed class EntityDropEntityData {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("dropSetIds")]
    public int[] DropSetIds = [];
}

internal sealed class EntityDropTableData {
    [JsonInclude, JsonPropertyName("variants")]
    public string[] Variants = [];

    [JsonInclude, JsonPropertyName("dropSets")]
    public EntityDrop[][] DropSets = [];

    [JsonInclude, JsonPropertyName("entities")]
    public EntityDropEntityData[] Entities = [];
}

[JsonSerializable(typeof(EntityDropTableData))]
internal partial class EntityDropJsonContext : JsonSerializerContext {
}
