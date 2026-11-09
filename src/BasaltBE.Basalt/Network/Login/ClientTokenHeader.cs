using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct ClientTokenHeader {
    [JsonPropertyName("alg")]
    public string Algorithm;

    [JsonPropertyName("x5u")]
    public string PublicKey;
}
