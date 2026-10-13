using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

public sealed class EntityRideableSeatsConverter : JsonConverter<EntityRideableSeat[]> {
    public override EntityRideableSeat[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.StartArray) {
            return JsonSerializer.Deserialize<EntityRideableSeat[]>(ref reader, options) ?? [];
        }

        if (reader.TokenType == JsonTokenType.StartObject) {
            EntityRideableSeat seat = JsonSerializer.Deserialize<EntityRideableSeat>(ref reader, options)
                ?? throw new JsonException("Rideable seat is empty.");
            return [seat];
        }

        throw new JsonException("Rideable seats must be an object or array.");
    }

    public override void Write(Utf8JsonWriter writer, EntityRideableSeat[] value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        foreach (EntityRideableSeat seat in value) {
            JsonSerializer.Serialize(writer, seat, options);
        }
        writer.WriteEndArray();
    }
}
