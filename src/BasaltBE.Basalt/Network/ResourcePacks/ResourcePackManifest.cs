using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.ResourcePacks;

internal sealed class ResourcePackManifest {
    [JsonPropertyName("header")]
    public ResourcePackManifestHeader? Header = new();
}
