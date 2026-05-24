using System.Net.Http.Json;
using System.Text.Json;

namespace Basalt.Waterfall.Auth;

public static class PlayFabAuth
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<PlayFabToken> LoginWithPlayFab(XstsToken xsts)
    {
        var payload = new
        {
            CreateAccount = true,
            TitleId = "20CA2",
            XboxToken = $"XBL3.0 x={xsts.UserHash};{xsts.Token}"
        };

        var response = await _httpClient.PostAsJsonAsync(AuthEndpoint.PlayFabLogin, payload);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"PlayFab login failed: {response.StatusCode} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var dataObj = data.GetProperty("data");

        return new PlayFabToken
        {
            SessionTicket = dataObj.GetProperty("SessionTicket").GetString() ?? "",
            EntityToken = dataObj.GetProperty("EntityToken").GetProperty("EntityToken").GetString() ?? "",
            ExpiresAt = DateTimeOffset.Parse(
                dataObj.GetProperty("EntityToken").GetProperty("TokenExpiration").GetString() ?? ""
            ).ToUnixTimeMilliseconds()
        };
    }
}
