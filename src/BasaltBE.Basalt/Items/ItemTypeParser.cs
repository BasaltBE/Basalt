using System.Text.Json;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Items;

public static class ItemTypeParser {
    static readonly JsonSerializerOptions Options = CreateOptions();

    public static ItemType[] Parse(Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.Deserialize<ItemType[]>(stream, Options)
            ?? throw new JsonException("Item type data is empty.");
    }

    static JsonSerializerOptions CreateOptions() {
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = false };
        options.Converters.Add(new ItemPropertiesPayloadJsonConverter());
        return options;
    }
}
