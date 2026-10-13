using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityInventoryComponent {
    [JsonInclude, JsonPropertyName("additional_slots_per_strength")]
    public int? AdditionalSlotsPerStrength;

    [JsonInclude, JsonPropertyName("can_be_siphoned_from")]
    public bool? CanBeSiphonedFrom;

    [JsonInclude, JsonPropertyName("container_type")]
    public string? ContainerType;

    [JsonInclude, JsonPropertyName("inventory_size")]
    public int? InventorySize;

    [JsonInclude, JsonPropertyName("private")]
    public bool? Private;

    [JsonInclude, JsonPropertyName("restrict_to_owner")]
    public bool? RestrictToOwner;
}
