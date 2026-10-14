using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemCatalog {
    [JsonInclude, JsonPropertyName("categoryName")]
    public string CategoryName = string.Empty;

    [JsonInclude, JsonPropertyName("group_identifier")]
    public ItemGroupIdentifier? GroupIdentifier;
}
