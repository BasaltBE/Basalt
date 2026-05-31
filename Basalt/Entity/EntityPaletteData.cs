namespace Basalt.Server.Entity;

using System.Text.Json.Serialization;

public sealed class EntityTypeData
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<EntityTypeData>))]
internal partial class EntityPaletteJsonContext : JsonSerializerContext
{
}






