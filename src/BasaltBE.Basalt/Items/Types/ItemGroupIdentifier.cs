using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemGroupIdentifier {
    [JsonInclude, JsonPropertyName("icon")]
    public string? Icon;

    [JsonInclude, JsonPropertyName("name")]
    public string? Name;
}
