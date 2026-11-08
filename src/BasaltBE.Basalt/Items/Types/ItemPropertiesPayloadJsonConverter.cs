using System.Text.Json;
using System.Text.Json.Serialization;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Items.Types;

internal sealed class ItemPropertiesPayloadJsonConverter : JsonConverter<ItemPropertiesPayload> {
    public override ItemPropertiesPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Item properties payload must be an object.");

        ItemPropertiesPayload payload = new();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) return payload;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected an item properties payload field.");

            string name = reader.GetString() ?? throw new JsonException("Item properties payload field name cannot be null.");
            if (!reader.Read()) throw new JsonException("Item properties payload ended before a field value.");

            switch (name) {
                case "components":
                    payload.Components = ReadStrings(ref reader);
                    break;
                case "item_tags":
                    payload.ItemTags = ReadStrings(ref reader);
                    break;
                case "stackable":
                    payload.Stackable = reader.TokenType switch {
                        JsonTokenType.True => true,
                        JsonTokenType.False => false,
                        _ => throw new JsonException("Item stackable value must be a boolean.")
                    };
                    break;
                case "maxAmount":
                    payload.MaxAmount = reader.GetInt32();
                    break;
                default:
                    payload.Values.Add(name, ReadTag(ref reader));
                    break;
            }
        }

        throw new JsonException("Item properties payload object was not closed.");
    }

    public override void Write(Utf8JsonWriter writer, ItemPropertiesPayload value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Item properties payload JSON is read-only.");

    static string[] ReadStrings(ref Utf8JsonReader reader) {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Item component names and tags must be arrays.");

        List<string> values = [];
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray) return values.ToArray();
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("Item component names and tags must be strings.");

            values.Add(reader.GetString() ?? string.Empty);
        }

        throw new JsonException("Item properties payload array was not closed.");
    }

    static CompoundTag ReadCompound(ref Utf8JsonReader reader) {
        CompoundTag compound = new();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) return compound;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected an item component field.");

            string name = reader.GetString() ?? throw new JsonException("Item component field name cannot be null.");
            if (!reader.Read()) throw new JsonException("Item component data ended before a field value.");

            compound.Add(name, ReadTag(ref reader));
        }

        throw new JsonException("Item component object was not closed.");
    }

    static Tag ReadTag(ref Utf8JsonReader reader) => reader.TokenType switch {
        JsonTokenType.StartObject => ReadCompound(ref reader),
        JsonTokenType.StartArray => ReadList(ref reader),
        JsonTokenType.String => new StringTag { Value = reader.GetString() ?? string.Empty },
        JsonTokenType.Number => ReadNumber(ref reader),
        JsonTokenType.True => new ByteTag { Value = 1 },
        JsonTokenType.False => new ByteTag { Value = 0 },
        JsonTokenType.Null => throw new JsonException("Item component data cannot contain null values."),
        _ => throw new JsonException($"Unexpected item component token {reader.TokenType}.")
    };

    static ListTag ReadList(ref Utf8JsonReader reader) {
        ListTag list = new();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray) return list;

            Tag item = ReadTag(ref reader);
            if (list.Value.Count == 0) list.ElementType = item.Type;
            else if (item.Type != list.ElementType) throw new JsonException("Item component lists must contain one tag type.");

            list.Value.Add(item);
        }

        throw new JsonException("Item component array was not closed.");
    }

    static Tag ReadNumber(ref Utf8JsonReader reader) {
        if (reader.TryGetInt32(out int intValue)) return new IntTag { Value = intValue };
        if (reader.TryGetInt64(out long longValue)) return new LongTag { Value = longValue };
        if (reader.TryGetSingle(out float floatValue)) return new FloatTag { Value = floatValue };
        if (reader.TryGetDouble(out double doubleValue)) return new DoubleTag { Value = doubleValue };

        throw new JsonException("Item component number is outside the supported NBT range.");
    }
}
