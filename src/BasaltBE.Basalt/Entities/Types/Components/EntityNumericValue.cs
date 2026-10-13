using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

[JsonConverter(typeof(EntityNumericValueConverter))]
public struct EntityNumericValue {
    public double? Value;
    public EntityNumericRange? Range;
}

public sealed class EntityNumericRange {
    [JsonInclude, JsonPropertyName("range_min")]
    public double RangeMin;

    [JsonInclude, JsonPropertyName("range_max")]
    public double RangeMax;
}

public sealed class EntityNumericValueConverter : JsonConverter<EntityNumericValue> {
    public override EntityNumericValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Number) {
            return new EntityNumericValue { Value = reader.GetDouble() };
        }

        if (reader.TokenType == JsonTokenType.StartObject) {
            EntityNumericRange range = JsonSerializer.Deserialize<EntityNumericRange>(ref reader, options)
                ?? throw new JsonException("Entity numeric range is empty.");
            return new EntityNumericValue { Range = range };
        }

        throw new JsonException("Entity numeric value must be a number or range.");
    }

    public override void Write(Utf8JsonWriter writer, EntityNumericValue value, JsonSerializerOptions options) {
        if (value.Range is not null) {
            JsonSerializer.Serialize(writer, value.Range, options);
            return;
        }

        if (value.Value is double number) {
            writer.WriteNumberValue(number);
            return;
        }

        throw new JsonException("Entity numeric value has no value.");
    }
}
