using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Basalt.Waterfall.Auth;

public static class XboxAuth
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<XblToken> AuthenticateXbl(string msAccessToken)
    {
        var rpsTicket = msAccessToken;

        var payload = new
        {
            RelyingParty = AuthEndpoint.XblAuthRelyingParty,
            TokenType = "JWT",
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = rpsTicket
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint.XblUserAuth)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("x-xbl-contract-version", "1");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"XBL authentication failed: {response.StatusCode}\nResponse: {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new XblToken
        {
            Token = data.GetProperty("Token").GetString() ?? "",
            UserHash = data.GetProperty("DisplayClaims")
                .GetProperty("xui")[0]
                .GetProperty("uhs").GetString() ?? "",
            ExpiresAt = DateTimeOffset.Parse(data.GetProperty("NotAfter").GetString() ?? "")
                .ToUnixTimeMilliseconds()
        };
    }

    public static async Task<XstsToken> AuthenticateXsts(string xblToken, string? relyingParty = null)
    {
        var payload = new
        {
            RelyingParty = relyingParty ?? AuthEndpoint.BedrockXSTSRelyingParty,
            TokenType = "JWT",
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint.XstsAuthorize)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorData = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (errorData.TryGetProperty("XErr", out var xErr))
                {
                    var errorCode = xErr.GetInt64();
                    if (XboxErrors.Messages.TryGetValue(errorCode, out var errorMessage))
                    {
                        throw new Exception(errorMessage);
                    }
                }
            }
            catch (JsonException) { }

            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"XSTS authentication failed: {response.StatusCode} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var xui = data.GetProperty("DisplayClaims").GetProperty("xui")[0];

        return new XstsToken
        {
            Token = data.GetProperty("Token").GetString() ?? "",
            UserHash = xui.GetProperty("uhs").GetString() ?? "",
            Gamertag = xui.TryGetProperty("gtg", out var gtg) ? gtg.GetString() ?? "" : "",
            Xuid = xui.TryGetProperty("xid", out var xid) ? xid.GetString() ?? "" : "",
            ExpiresAt = DateTimeOffset.Parse(data.GetProperty("NotAfter").GetString() ?? "")
                .ToUnixTimeMilliseconds()
        };
    }
}
