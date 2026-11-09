using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct LoginIdentity {
    [JsonPropertyName("AuthenticationType")]
    public int? AuthenticationType;

    [JsonPropertyName("Token")]
    public string Token;
}
