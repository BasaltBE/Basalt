using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal struct MultiplayerTokenPayload {
    [JsonPropertyName("cpk")]
    public JsonElement ClientPublicKey;

    [JsonPropertyName("xid")]
    public string Xuid;

    [JsonPropertyName("xname")]
    public string Username;

    [JsonPropertyName("exp")]
    public long ExpiresAt;

    [JsonPropertyName("nbf")]
    public long NotBefore;

    [JsonPropertyName("iss")]
    public string Issuer;

    [JsonPropertyName("aud")]
    public JsonElement Audience;
}
