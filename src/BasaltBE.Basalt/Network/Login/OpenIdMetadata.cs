using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct OpenIdMetadata {
    [JsonPropertyName("issuer")]
    public string Issuer;

    [JsonPropertyName("jwks_uri")]
    public string KeysUrl;
}
