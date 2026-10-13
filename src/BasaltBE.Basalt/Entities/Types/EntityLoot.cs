using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types;

public sealed class EntityLoot {
    [JsonInclude, JsonPropertyName("table")]
    public string Table = string.Empty;
}
