using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Commands.Types;

public sealed class EnchantmentOption
{
    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;
    [JsonInclude, JsonPropertyName("id")]
    public int Id;
    [JsonInclude, JsonPropertyName("maxLevel")]
    public int MaximumLevel;
}
