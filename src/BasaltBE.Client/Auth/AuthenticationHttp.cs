using System.Text;
using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal static class AuthenticationHttp {
    internal static async Task<JsonDocument> PostJsonAsync(
        HttpClient httpClient,
        string endpoint,
        object body,
        CancellationToken cancellationToken,
        params KeyValuePair<string, string>[] headers
    ) {
        using HttpRequestMessage request = new(HttpMethod.Post, endpoint) {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        foreach ((string name, string value) in headers) {
            if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)) {
                request.Headers.TryAddWithoutValidation(name, value);
            }
            else if (name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)) {
                request.Headers.UserAgent.ParseAdd(value);
            }
            else {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }
        using HttpResponseMessage response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        JsonDocument document = await AuthenticationJson.ReadAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            string detail = $"{new Uri(endpoint).Host} returned HTTP {(int)response.StatusCode}";
            if (document.RootElement.TryGetProperty("errorCode", out JsonElement code) &&
                code.TryGetInt32(out int errorCode)) {
                detail += $" (service code {errorCode})";
            }

            if (document.RootElement.TryGetProperty("error", out JsonElement error) &&
                error.ValueKind == JsonValueKind.String &&
                error.GetString() is { Length: > 0 and <= 64 } errorName && errorName.All(char.IsAsciiLetterOrDigit))
                detail += $": {errorName}";
            document.Dispose();
            throw new HttpRequestException(detail + ".");
        }
        return document;
    }
}
