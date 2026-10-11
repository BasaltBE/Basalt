using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct OpenIdSigningKey {
    [JsonPropertyName("kid")]
    public string KeyId;

    [JsonPropertyName("alg")]
    public string Algorithm;

    [JsonPropertyName("kty")]
    public string KeyType;

    [JsonPropertyName("n")]
    public string Modulus;

    [JsonPropertyName("e")]
    public string Exponent;

    [JsonPropertyName("x")]
    public string X;

    [JsonPropertyName("y")]
    public string Y;
}
