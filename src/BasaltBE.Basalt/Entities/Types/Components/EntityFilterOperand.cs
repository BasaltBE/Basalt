using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Entities.Types.Components;

[JsonConverter(typeof(EntityFilterOperandConverter))]
public struct EntityFilterOperand {
    public int? Number;
    public string? Text;
}

public sealed class EntityFilterOperandConverter : JsonConverter<EntityFilterOperand> {
    public override EntityFilterOperand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch {
            JsonTokenType.Number => new EntityFilterOperand { Number = reader.GetInt32() },
            JsonTokenType.String => new EntityFilterOperand { Text = reader.GetString() },
            _ => throw new JsonException("Entity filter operand must be an integer or string.")
        };

    public override void Write(Utf8JsonWriter writer, EntityFilterOperand value, JsonSerializerOptions options) {
        if (value.Number is int number) {
            writer.WriteNumberValue(number);
            return;
        }

        if (value.Text is not null) {
            writer.WriteStringValue(value.Text);
            return;
        }

        throw new JsonException("Entity filter operand has no value.");
    }
}
