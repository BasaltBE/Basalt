using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.ResourcePacks;

internal sealed class ResourcePackManifestHeader {
    [JsonPropertyName("uuid")]
    public string Uuid = string.Empty;

    [JsonPropertyName("name")]
    public string Name = string.Empty;

    [JsonPropertyName("description")]
    public string Description = string.Empty;

    [JsonPropertyName("version")]
    public int[] Version = [];
}
