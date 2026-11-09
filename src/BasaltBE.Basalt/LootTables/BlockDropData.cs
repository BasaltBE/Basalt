using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.LootTables;

internal sealed class BlockDropBlockData {
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("dropSetIds")]
    public int[] DropSetIds = [];
}

internal sealed class BlockDropTableData {
    [JsonInclude, JsonPropertyName("tools")]
    public string[] Tools = [];

    [JsonInclude, JsonPropertyName("variants")]
    public string[] Variants = [];

    [JsonInclude, JsonPropertyName("dropSets")]
    public BlockDrop[][] DropSets = [];

    [JsonInclude, JsonPropertyName("blocks")]
    public BlockDropBlockData[] Blocks = [];
}

[JsonSerializable(typeof(BlockDropTableData))]
internal partial class BlockDropJsonContext : JsonSerializerContext {
}
