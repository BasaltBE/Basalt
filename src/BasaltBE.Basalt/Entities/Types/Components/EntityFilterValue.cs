using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

[JsonConverter(typeof(EntityFilterValueConverter))]
public struct EntityFilterValue {
    public bool? Boolean;
    public string? Text;
}

public sealed class EntityFilterValueConverter : JsonConverter<EntityFilterValue> {
    public override EntityFilterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch {
            JsonTokenType.True => new EntityFilterValue { Boolean = true },
            JsonTokenType.False => new EntityFilterValue { Boolean = false },
            JsonTokenType.String => new EntityFilterValue { Text = reader.GetString() },
            _ => throw new JsonException("Entity filter value must be a boolean or string.")
        };

    public override void Write(Utf8JsonWriter writer, EntityFilterValue value, JsonSerializerOptions options) {
        if (value.Boolean is bool boolean) {
            writer.WriteBooleanValue(boolean);
            return;
        }

        if (value.Text is not null) {
            writer.WriteStringValue(value.Text);
            return;
        }

        throw new JsonException("Entity filter value has no value.");
    }
}
