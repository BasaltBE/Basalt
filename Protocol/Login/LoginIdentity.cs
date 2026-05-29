using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Basalt.Protocol.Login;

public readonly record struct VerifiedIdentity(string IdentityPublicKey, string Username, string Xuid, string Uuid);

public static class LoginIdentity
{
    private const string ConfigUrl = "https://authorization.franchise.minecraft-services.net/.well-known/openid-configuration";
    private const string AudienceApi = "api://auth-minecraft-services/multiplayer";

    private static readonly HttpClient Http = new();
    private static readonly Lock AuthLock = new();
    private static AuthConfig? CachedAuth;

    public static VerifiedIdentity Verify(string identityJson)
    {
        using JsonDocument envelope = JsonDocument.Parse(identityJson);
        string token = ResolveToken(envelope.RootElement);

        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Missing identity token.");

        TokenParts parts = ParseTokenParts(token);
        VerifyServiceToken(token.AsSpan(), parts);

        byte[] payloadBytes = JsonValue.DecodeBase64Url(token.AsSpan(parts.PayloadStart, parts.PayloadLength));
        using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);
        JsonElement payload = payloadDoc.RootElement;

        string uuid = JsonValue.GetString(payload, "identity")
            .Or(JsonValue.GetString(payload, "uuid"))
            .Or(JsonValue.GetString(payload, "sub"));

        return new VerifiedIdentity(
            JsonValue.GetString(payload, "cpk"),
            JsonValue.GetString(payload, "xname"),
            JsonValue.GetString(payload, "xid"),
            uuid
        );
    }

    private static void VerifyServiceToken(ReadOnlySpan<char> token, TokenParts parts)
    {
        byte[] headerBytes = JsonValue.DecodeBase64Url(token.Slice(parts.HeaderStart, parts.HeaderLength));
        byte[] payloadBytes = JsonValue.DecodeBase64Url(token.Slice(parts.PayloadStart, parts.PayloadLength));

        using JsonDocument headerDoc = JsonDocument.Parse(headerBytes);
        using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);

        JsonElement header = headerDoc.RootElement;
        JsonElement payload = payloadDoc.RootElement;

        string alg = JsonValue.GetString(header, "alg");
        string kid = JsonValue.GetString(header, "kid");
        string typ = JsonValue.GetString(header, "typ");

        if (!string.Equals(alg, "RS256", StringComparison.Ordinal))
            throw new InvalidOperationException("Unsupported authentication algorithm.");

        if (!string.IsNullOrEmpty(typ) && !string.Equals(typ, "JWT", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid token type.");

        if (JsonValue.GetInt64(payload, "exp") <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new InvalidOperationException("Authentication expired.");

        if (!string.Equals(JsonValue.GetString(payload, "aud"), AudienceApi, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid audience.");

        AuthConfig config = GetAuthConfig();

        if (!config.Algorithms.Contains(alg))
            throw new InvalidOperationException("Algorithm not allowed by authority.");

        if (!string.Equals(JsonValue.GetString(payload, "iss"), config.Issuer, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid issuer.");

        if (!config.Keys.TryGetValue(kid, out RSA? key))
            throw new InvalidOperationException("Unknown key id.");

        JwtVerification.TokenParts jwtParts = new(
            parts.HeaderStart,
            parts.HeaderLength,
            parts.PayloadStart,
            parts.PayloadLength,
            parts.SignatureStart,
            parts.SignatureLength);

        JwtVerification.VerifyRsa256Signature(
            token,
            jwtParts,
            key,
            JsonValue.DecodeBase64Url(token.Slice(parts.SignatureStart, parts.SignatureLength)));
    }

    private static AuthConfig GetAuthConfig()
    {
        lock (AuthLock)
        {
            if (CachedAuth is not null && CachedAuth.ExpiresAt > DateTimeOffset.UtcNow)
                return CachedAuth;

            string configJson = Http.GetStringAsync(ConfigUrl).GetAwaiter().GetResult();
            using JsonDocument configDoc = JsonDocument.Parse(configJson);
            JsonElement configRoot = configDoc.RootElement;

            HashSet<string> algorithms = JsonValue.GetArray(configRoot, "id_token_signing_alg_values_supported")
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToHashSet(StringComparer.Ordinal);

            string jwksJson = Http.GetStringAsync(JsonValue.GetString(configRoot, "jwks_uri")).GetAwaiter().GetResult();
            using JsonDocument jwksDoc = JsonDocument.Parse(jwksJson);

            Dictionary<string, RSA> keys = JsonValue.GetArray(jwksDoc.RootElement, "keys")
                .Where(jwk =>
                    string.Equals(JsonValue.GetString(jwk, "kty"), "RSA", StringComparison.Ordinal) &&
                    !string.IsNullOrEmpty(JsonValue.GetString(jwk, "kid")) &&
                    !string.IsNullOrEmpty(JsonValue.GetString(jwk, "n")) &&
                    !string.IsNullOrEmpty(JsonValue.GetString(jwk, "e")))
                .ToDictionary(
                    jwk => JsonValue.GetString(jwk, "kid"),
                    jwk =>
                    {
                        RSA rsa = RSA.Create();
                        rsa.ImportParameters(new RSAParameters
                        {
                            Modulus = JsonValue.DecodeBase64Url(JsonValue.GetString(jwk, "n")),
                            Exponent = JsonValue.DecodeBase64Url(JsonValue.GetString(jwk, "e"))
                        });
                        return rsa;
                    },
                    StringComparer.Ordinal);

            return CachedAuth = new AuthConfig(
                JsonValue.GetString(configRoot, "issuer"),
                algorithms,
                keys,
                DateTimeOffset.UtcNow.AddHours(1)
            );
        }
    }

    private static string ResolveToken(JsonElement root)
    {
        foreach (string name in (string[])["Token", "token", "AuthorizationToken", "authorizationToken"])
        {
            if (root.TryGetProperty(name, out JsonElement val) && val.ValueKind == JsonValueKind.String)
                return val.GetString() ?? string.Empty;
        }

        foreach (string name in (string[])["chain", "Chain"])
        {
            if (root.TryGetProperty(name, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                return arr.EnumerateArray().LastOrDefault(e => e.ValueKind == JsonValueKind.String).GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static TokenParts ParseTokenParts(string token)
    {
        int firstDot = token.IndexOf('.');
        int secondDot = firstDot > 0 ? token.IndexOf('.', firstDot + 1) : -1;

        if (firstDot <= 0
            || secondDot <= firstDot + 1
            || secondDot == token.Length - 1
            || token.IndexOf('.', secondDot + 1) >= 0)
        {
            throw new InvalidOperationException("Malformed identity token.");
        }

        return new TokenParts(0, firstDot, firstDot + 1, secondDot - firstDot - 1, secondDot + 1, token.Length - secondDot - 1);
    }

    private static string Or(this string value, string fallback) =>
        string.IsNullOrEmpty(value) ? fallback : value;

    private readonly record struct TokenParts(
        int HeaderStart, int HeaderLength,
        int PayloadStart, int PayloadLength,
        int SignatureStart, int SignatureLength
    );

    private sealed class AuthConfig(string issuer, HashSet<string> algorithms, Dictionary<string, RSA> keys, DateTimeOffset expiresAt)
    {
        public string Issuer { get; } = issuer;
        public HashSet<string> Algorithms { get; } = algorithms;
        public Dictionary<string, RSA> Keys { get; } = keys;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
    }
}