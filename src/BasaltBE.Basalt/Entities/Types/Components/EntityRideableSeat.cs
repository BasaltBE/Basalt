using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityRideableSeat {
    [JsonInclude, JsonPropertyName("position")]
    public double[] Position = [];

    [JsonInclude, JsonPropertyName("min_rider_count")]
    public int? MinRiderCount;

    [JsonInclude, JsonPropertyName("max_rider_count")]
    public int? MaxRiderCount;

    [JsonInclude, JsonPropertyName("lock_rider_rotation")]
    public double? LockRiderRotation;

    [JsonInclude, JsonPropertyName("rotate_rider_by")]
    public double? RotateRiderBy;
}
