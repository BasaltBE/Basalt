using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityTypeFamilyComponent {
    [JsonInclude, JsonPropertyName("family")]
    public string[] Family = [];
}
