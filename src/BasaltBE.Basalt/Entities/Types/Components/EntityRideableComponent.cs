using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityRideableComponent {
    [JsonInclude, JsonPropertyName("family_types")]
    public string[]? FamilyTypes;

    [JsonInclude, JsonPropertyName("interact_text")]
    public string? InteractText;

    [JsonInclude, JsonPropertyName("pull_in_entities")]
    public bool? PullInEntities;

    [JsonInclude, JsonPropertyName("seat_count")]
    public int? SeatCount;

    [JsonInclude, JsonPropertyName("seats"), JsonConverter(typeof(EntityRideableSeatsConverter))]
    public EntityRideableSeat[]? Seats;
}
