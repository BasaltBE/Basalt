using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal sealed class XboxAuthenticator(HttpClient httpClient) {
    private const string XboxAuthEndpoint = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string BedrockRelyingParty = "https://multiplayer.minecraft.net/";
    private const string PlayFabRelyingParty = "https://b980a380.minecraft.playfabapi.com/";

    internal async Task<XboxAuthentication> AuthenticateAsync(
        string accessToken,
        CancellationToken cancellationToken
    ) {
        using JsonDocument document = await AuthenticationHttp.PostJsonAsync(
            httpClient,
            XboxAuthEndpoint,
            new {
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT",
                Properties = new {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = accessToken
                }
            },
            cancellationToken,
            new KeyValuePair<string, string>("x-xbl-contract-version", "1")
        ).ConfigureAwait(false);
        string xboxToken = AuthenticationJson.RequiredString(document.RootElement, "Token");
        XstsToken bedrock = await AuthorizeXstsAsync(
            xboxToken,
            BedrockRelyingParty,
            cancellationToken
        ).ConfigureAwait(false);
        XstsToken playFab = await AuthorizeXstsAsync(
            xboxToken,
            PlayFabRelyingParty,
            cancellationToken
        ).ConfigureAwait(false);

        return new XboxAuthentication(
            new BedrockAccount(bedrock.Gamertag, bedrock.Xuid),
            $"XBL3.0 x={playFab.UserHash};{playFab.Token}"
        );
    }

    private async Task<XstsToken> AuthorizeXstsAsync(
        string xboxToken,
        string relyingParty,
        CancellationToken cancellationToken
    ) {
        using JsonDocument document = await AuthenticationHttp.PostJsonAsync(
            httpClient,
            XstsEndpoint,
            new {
                RelyingParty = relyingParty,
                TokenType = "JWT",
                Properties = new { SandboxId = "RETAIL", UserTokens = new[] { xboxToken } }
            },
            cancellationToken
        ).ConfigureAwait(false);

        JsonElement data = document.RootElement;
        JsonElement xui = data.GetProperty("DisplayClaims").GetProperty("xui")[0];
        return new XstsToken(
            AuthenticationJson.RequiredString(data, "Token"),
            AuthenticationJson.RequiredString(xui, "uhs"),
            AuthenticationJson.GetString(xui, "gtg"),
            AuthenticationJson.GetString(xui, "xid")
        );
    }

    private sealed record XstsToken(string Token, string UserHash, string Gamertag, string Xuid);
}

internal sealed record XboxAuthentication(BedrockAccount BedrockAccount, string PlayFabAuthorization);
