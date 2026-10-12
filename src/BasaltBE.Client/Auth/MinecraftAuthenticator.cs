using System.Globalization;
using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal sealed class MinecraftAuthenticator(HttpClient httpClient) {
    internal async Task<string> AuthenticateAsync(
        string playFabAuthorization,
        string publicKey,
        CancellationToken cancellationToken
    ) {
        string playFabSession = await LoginPlayFabAsync(playFabAuthorization, cancellationToken).ConfigureAwait(false);
        string minecraftAuthorization = await StartMinecraftServicesSessionAsync(
            playFabSession,
            cancellationToken
        ).ConfigureAwait(false);

        return await StartMultiplayerSessionAsync(
            minecraftAuthorization,
            publicKey,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<string> LoginPlayFabAsync(string xboxAuthorization, CancellationToken cancellationToken) {
        using JsonDocument document = await AuthenticationHttp.PostJsonAsync(
            httpClient,
            "https://20ca2.playfabapi.com/Client/LoginWithXbox",
            new { CreateAccount = true, TitleId = "20CA2", XboxToken = xboxAuthorization },
            cancellationToken
        ).ConfigureAwait(false);

        return AuthenticationJson.RequiredString(document.RootElement.GetProperty("data"), "SessionTicket");
    }

    private async Task<string> StartMinecraftServicesSessionAsync(
        string playFabSession,
        CancellationToken cancellationToken
    ) {
        using JsonDocument document = await AuthenticationHttp.PostJsonAsync(
            httpClient,
            "https://authorization.franchise.minecraft-services.net/api/v1.0/session/start",
            new {
                device = new {
                    applicationType = "MinecraftPE",
                    gameVersion = Protocol.BedrockProtocol.MinecraftVersion,
                    id = "c1681ad3-415e-30cd-abd3-3b8f51e771d1",
                    memory = (8L * 1024 * 1024 * 1024).ToString(CultureInfo.InvariantCulture),
                    platform = "Windows10",
                    playFabTitleId = "20CA2",
                    storePlatform = "uwp.store",
                    type = "Windows10"
                },
                user = new { token = playFabSession, tokenType = "PlayFab" }
            },
            cancellationToken
        ).ConfigureAwait(false);

        return AuthenticationJson.RequiredString(document.RootElement.GetProperty("result"), "authorizationHeader");
    }

    private async Task<string> StartMultiplayerSessionAsync(
        string authorization,
        string publicKey,
        CancellationToken cancellationToken
    ) {
        using JsonDocument document = await AuthenticationHttp.PostJsonAsync(
            httpClient,
            "https://authorization.franchise.minecraft-services.net/api/v1.0/multiplayer/session/start",
            new { publicKey },
            cancellationToken,
            new KeyValuePair<string, string>("Authorization", authorization),
            new KeyValuePair<string, string>("Accept-Encoding", "identity")
        ).ConfigureAwait(false);

        return AuthenticationJson.RequiredString(document.RootElement.GetProperty("result"), "signedToken");
    }
}
