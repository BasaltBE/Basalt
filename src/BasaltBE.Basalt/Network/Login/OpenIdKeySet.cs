using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct OpenIdKeySet {
    [JsonPropertyName("keys")]
    public OpenIdSigningKey[] Keys;
}
