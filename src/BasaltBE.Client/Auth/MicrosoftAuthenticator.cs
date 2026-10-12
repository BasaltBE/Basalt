using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal sealed class MicrosoftAuthenticator(HttpClient httpClient) {
    private const string ClientId = "000000004C12AE6F";
    private const string DeviceCodeEndpoint = "https://login.live.com/oauth20_connect.srf";
    private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
    private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";

    internal async Task<MicrosoftToken> AuthenticateAsync(
        Action<MicrosoftDeviceCode> showCode,
        CancellationToken cancellationToken
    ) {
        using HttpResponseMessage response = await httpClient.PostAsync(
            DeviceCodeEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> {
                ["client_id"] = ClientId,
                ["scope"] = Scope,
                ["response_type"] = "device_code"
            }),
            cancellationToken
        ).ConfigureAwait(false);
        using JsonDocument document = await AuthenticationJson
            .ReadAsync(response, cancellationToken)
            .ConfigureAwait(false);
        JsonElement data = document.RootElement;
        EnsureSuccess(response, data);

        string deviceCode = AuthenticationJson.RequiredString(data, "device_code");
        string userCode = AuthenticationJson.RequiredString(data, "user_code");
        Uri verificationUri = new(AuthenticationJson.RequiredString(data, "verification_uri"));
        string message = data.TryGetProperty("message", out JsonElement messageValue)
            ? messageValue.GetString() ?? $"Open {verificationUri} and enter code {userCode}."
            : $"Open {verificationUri} and enter code {userCode}.";
        showCode(new MicrosoftDeviceCode(userCode, verificationUri, message));
        int interval = data.TryGetProperty("interval", out JsonElement intervalValue)
            ? Math.Max(1, intervalValue.GetInt32())
            : 5;
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddSeconds(AuthenticationJson.RequiredInt32(data, "expires_in"));

        while (DateTimeOffset.UtcNow < expires) {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken).ConfigureAwait(false);
            using HttpResponseMessage tokenResponse = await httpClient.PostAsync(
                TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["client_id"] = ClientId,
                    ["device_code"] = deviceCode
                }),
                cancellationToken
            ).ConfigureAwait(false);
            using JsonDocument tokenDocument = await AuthenticationJson
                .ReadAsync(tokenResponse, cancellationToken)
                .ConfigureAwait(false);
            JsonElement tokenData = tokenDocument.RootElement;
            if (tokenResponse.IsSuccessStatusCode) {
                return ReadToken(tokenData);
            }

            string code = AuthenticationJson.GetString(tokenData, "error");
            if (code == "authorization_pending") {
                continue;
            }

            if (code == "slow_down") {
                interval += 5;
                continue;
            }

            throw new MicrosoftTokenException(code, "Microsoft device sign-in failed.");
        }
        throw new TimeoutException("Microsoft device sign-in expired before it completed.");
    }

    internal async Task<MicrosoftToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await httpClient.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["refresh_token"] = refreshToken,
                ["scope"] = Scope
            }),
            cancellationToken
        ).ConfigureAwait(false);
        using JsonDocument document = await AuthenticationJson
            .ReadAsync(response, cancellationToken)
            .ConfigureAwait(false);
        EnsureSuccess(response, document.RootElement);
        return ReadToken(document.RootElement);
    }

    private static MicrosoftToken ReadToken(JsonElement data) => new(
        AuthenticationJson.RequiredString(data, "access_token"),
        AuthenticationJson.RequiredString(data, "refresh_token")
    );

    private static void EnsureSuccess(HttpResponseMessage response, JsonElement data) {
        if (!response.IsSuccessStatusCode) {
            throw new MicrosoftTokenException(
                AuthenticationJson.GetString(data, "error"),
                "Microsoft authentication request failed."
            );
        }
    }
}

internal sealed record MicrosoftToken(string AccessToken, string RefreshToken);

internal sealed class MicrosoftTokenException(string code, string message) : Exception(message) {
    internal string Code { get; } = code;
}
