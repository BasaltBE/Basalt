using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types;

public sealed class EntityTypePayload {
    [JsonInclude, JsonPropertyName("components")]
    public EntityComponentPayloads Components = new();
}
