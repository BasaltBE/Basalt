using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct MultiplayerTokenHeader {
    [JsonPropertyName("alg")]
    public string Algorithm;

    [JsonPropertyName("kid")]
    public string KeyId;
}
