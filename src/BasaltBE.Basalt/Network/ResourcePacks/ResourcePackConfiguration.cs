using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.ResourcePacks;

internal sealed class ResourcePackConfiguration {
    [JsonPropertyName("required")]
    public bool Required;

    [JsonPropertyName("enabledPacks")]
    public string[]? EnabledPacks;
}
