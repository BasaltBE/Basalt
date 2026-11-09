using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal static class AuthenticationJson {
    internal static async Task<JsonDocument> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    ) {
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    internal static string RequiredString(JsonElement data, string name) {
        if (data.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString())) {
            return value.GetString()!;
        }

        throw new InvalidDataException($"Microsoft authentication response is missing {name}.");
    }

    internal static int RequiredInt32(JsonElement data, string name) {
        if (data.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int number) && number > 0) {
            return number;
        }

        throw new InvalidDataException($"Microsoft authentication response is missing {name}.");
    }

    internal static string GetString(JsonElement data, string name) {
        if (!data.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String) {
            return "";
        }

        return value.GetString() ?? "";
    }
}
