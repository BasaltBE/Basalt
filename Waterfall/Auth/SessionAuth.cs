using System.Net.Http.Json;
using System.Text.Json;

namespace Basalt.Waterfall.Auth;

public static class SessionAuth
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<List<string>> GetBedrockChain(
        string xstsToken,
        string userHash,
        string clientPublicKey)
    {
        var payload = new
        {
            identityPublicKey = clientPublicKey
        };

        var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint.BedrockAuthenticate)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("User-Agent", "MCPE/UWP");
        request.Headers.Add("Authorization", $"XBL3.0 x={userHash};{xstsToken}");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"Bedrock authentication failed: {response.StatusCode} {response.ReasonPhrase} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var chain = new List<string>();
        
        foreach (var item in data.GetProperty("chain").EnumerateArray())
        {
            chain.Add(item.GetString() ?? "");
        }

        return chain;
    }

    public static async Task<McServicesToken> GetMinecraftServicesToken(string sessionTicket)
    {
        var payload = new
        {
            device = new
            {
                applicationType = "MinecraftPE",
                gameVersion = "1.21.130",
                id = "c1681ad3-415e-30cd-abd3-3b8f51e771d1",
                memory = (8L * 1024 * 1024 * 1024).ToString(),
                platform = "Windows10",
                playFabTitleId = "20CA2",
                storePlatform = "uwp.store",
                type = "Windows10"
            },
            user = new
            {
                token = sessionTicket,
                tokenType = "PlayFab"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(AuthEndpoint.BedrockServicesSessionStart, payload);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"MC services token failed: {response.StatusCode} {response.ReasonPhrase} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new McServicesToken
        {
            AuthorizationHeader = data.GetProperty("result").GetProperty("authorizationHeader").GetString() ?? "",
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (24 * 60 * 60 * 1000)
        };
    }

    public static async Task<MultiplayerSessionToken> GetMultiplayerSessionToken(
        string mcServicesAuth,
        string clientPublicKey)
    {
        var payload = new
        {
            publicKey = clientPublicKey
        };

        var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint.BedrockMultiplayerSessionStart)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Authorization", mcServicesAuth);
        request.Headers.Add("Accept-Encoding", "identity");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"Multiplayer session start failed: {response.StatusCode} {response.ReasonPhrase} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new MultiplayerSessionToken
        {
            SignedToken = data.GetProperty("result").GetProperty("signedToken").GetString() ?? "",
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (24 * 60 * 60 * 1000)
        };
    }
}
