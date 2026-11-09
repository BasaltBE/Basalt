using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types;

public sealed class BlockStateConverter : JsonConverter<BlockState> {
    public override BlockState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new JsonException("Block state must be a JSON object.");
        }

        BlockState state = new();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                return state;
            }

            if (reader.TokenType != JsonTokenType.PropertyName) {
                throw new JsonException("Block state must contain named values.");
            }

            string name = reader.GetString()!;
            if (!reader.Read()) {
                throw new JsonException("Block state value is missing.");
            }

            object value = reader.TokenType switch {
                JsonTokenType.String => reader.GetString()!,
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt32(out int intValue) => intValue,
                JsonTokenType.Number when reader.TryGetInt64(out long longValue) => longValue,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.Null => null!,
                _ => throw new JsonException("Block state values must be simple JSON values.")
            };

            state.Values.Add(name, value);
        }

        throw new JsonException("Block state object is incomplete.");
    }

    public override void Write(Utf8JsonWriter writer, BlockState value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        foreach ((string name, object stateValue) in value.Values) {
            writer.WritePropertyName(name);
            switch (stateValue) {
                case string stringValue:
                    writer.WriteStringValue(stringValue);
                    break;
                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    break;
                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;
                case long longValue:
                    writer.WriteNumberValue(longValue);
                    break;
                case double doubleValue:
                    writer.WriteNumberValue(doubleValue);
                    break;
                case null:
                    writer.WriteNullValue();
                    break;
                default:
                    throw new JsonException("Block state values must be simple JSON values.");
            }
        }
        writer.WriteEndObject();
    }
}
