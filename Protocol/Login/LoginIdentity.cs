using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Basalt.Protocol.Login;

public readonly record struct VerifiedIdentity(string IdentityPublicKey, string Username, string Xuid, string Uuid);

public static class LoginIdentityVerifier
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
        {
            throw new InvalidOperationException("Missing identity token.");
        }

        TokenParts parts = ParseTokenParts(token);
        VerifyServiceToken(token.AsSpan(), parts);

        byte[] payloadBytes = DecodeBase64Url(token.AsSpan(parts.PayloadStart, parts.PayloadLength));
        try
        {
            using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);
            JsonElement payload = payloadDoc.RootElement;

            string identityPublicKey = GetString(payload, "cpk");
            string username = GetString(payload, "xname");
            string xuid = GetString(payload, "xid");

            string uuid = GetString(payload, "identity");
            if (string.IsNullOrEmpty(uuid)) uuid = GetString(payload, "uuid");
            if (string.IsNullOrEmpty(uuid)) uuid = GetString(payload, "sub");

            return new VerifiedIdentity(identityPublicKey, username, xuid, uuid);
        }
        finally
        {
            Array.Clear(payloadBytes);
        }
    }

    private static void VerifyServiceToken(ReadOnlySpan<char> token, TokenParts parts)
    {
        byte[] headerBytes = DecodeBase64Url(token.Slice(parts.HeaderStart, parts.HeaderLength));
        byte[] payloadBytes = DecodeBase64Url(token.Slice(parts.PayloadStart, parts.PayloadLength));

        try
        {
            using JsonDocument headerDoc = JsonDocument.Parse(headerBytes);
            using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);

            JsonElement header = headerDoc.RootElement;
            JsonElement payload = payloadDoc.RootElement;

            string alg = GetString(header, "alg");
            string kid = GetString(header, "kid");
            string typ = GetString(header, "typ");

            if (!string.Equals(alg, "RS256", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unsupported authentication algorithm.");
            }

            if (!string.IsNullOrEmpty(typ) && !string.Equals(typ, "JWT", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid token type.");
            }

            long expiresAt = GetInt64(payload, "exp");
            string audience = GetString(payload, "aud");
            string issuer = GetString(payload, "iss");
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (expiresAt <= now)
            {
                throw new InvalidOperationException("Authentication expired.");
            }

            if (!string.Equals(audience, AudienceApi, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid audience.");
            }

            AuthConfig config = GetAuthConfig();

            if (!config.Algorithms.Contains(alg))
            {
                throw new InvalidOperationException("Algorithm not allowed by authority.");
            }

            if (!string.Equals(issuer, config.Issuer, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid issuer.");
            }

            if (!config.Keys.TryGetValue(kid, out RSA? key))
            {
                throw new InvalidOperationException("Unknown key id.");
            }

            byte[] signature = DecodeBase64Url(token.Slice(parts.SignatureStart, parts.SignatureLength));
            try
            {
                byte[] signingInput = ArrayPool<byte>.Shared.Rent(parts.HeaderLength + 1 + parts.PayloadLength);
                try
                {
                    int written = Encoding.ASCII.GetBytes(token.Slice(parts.HeaderStart, parts.HeaderLength), signingInput);
                    signingInput[written++] = (byte)'.';
                    written += Encoding.ASCII.GetBytes(token.Slice(parts.PayloadStart, parts.PayloadLength), signingInput.AsSpan(written));

                    if (!key.VerifyData(signingInput.AsSpan(0, written), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    {
                        throw new InvalidOperationException("Invalid token signature.");
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(signingInput, clearArray: true);
                }
            }
            finally
            {
                Array.Clear(signature);
            }
        }
        finally
        {
            Array.Clear(headerBytes);
            Array.Clear(payloadBytes);
        }
    }

    private static AuthConfig GetAuthConfig()
    {
        lock (AuthLock)
        {
            if (CachedAuth is not null && CachedAuth.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return CachedAuth;
            }

            string configJson = Http.GetStringAsync(ConfigUrl).GetAwaiter().GetResult();
            using JsonDocument configDoc = JsonDocument.Parse(configJson);
            JsonElement configRoot = configDoc.RootElement;

            string jwksUri = GetString(configRoot, "jwks_uri");
            string issuer = GetString(configRoot, "issuer");

            HashSet<string> algorithms = new(StringComparer.Ordinal);
            if (configRoot.TryGetProperty("id_token_signing_alg_values_supported", out JsonElement algs) && algs.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement alg in algs.EnumerateArray())
                {
                    if (alg.ValueKind == JsonValueKind.String)
                    {
                        algorithms.Add(alg.GetString() ?? string.Empty);
                    }
                }
            }

            string jwksJson = Http.GetStringAsync(jwksUri).GetAwaiter().GetResult();
            using JsonDocument jwksDoc = JsonDocument.Parse(jwksJson);

            Dictionary<string, RSA> keys = new(StringComparer.Ordinal);
            if (jwksDoc.RootElement.TryGetProperty("keys", out JsonElement jwksKeys) && jwksKeys.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement jwk in jwksKeys.EnumerateArray())
                {
                    string kid = GetString(jwk, "kid");
                    string kty = GetString(jwk, "kty");
                    string n = GetString(jwk, "n");
                    string e = GetString(jwk, "e");

                    if (!string.Equals(kty, "RSA", StringComparison.Ordinal) || string.IsNullOrEmpty(kid) || string.IsNullOrEmpty(n) || string.IsNullOrEmpty(e))
                    {
                        continue;
                    }

                    RSA rsa = RSA.Create();
                    rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = DecodeBase64Url(n),
                        Exponent = DecodeBase64Url(e)
                    });

                    keys[kid] = rsa;
                }
            }

            CachedAuth = new AuthConfig(issuer, algorithms, keys, DateTimeOffset.UtcNow.AddHours(1));
            return CachedAuth;
        }
    }

    private static TokenParts ParseTokenParts(string token)
    {
        int firstDot = token.IndexOf('.');
        if (firstDot <= 0)
        {
            throw new InvalidOperationException("Malformed identity token.");
        }

        int secondDot = token.IndexOf('.', firstDot + 1);
        if (secondDot <= firstDot + 1 || secondDot == token.Length - 1)
        {
            throw new InvalidOperationException("Malformed identity token.");
        }

        if (token.IndexOf('.', secondDot + 1) >= 0)
        {
            throw new InvalidOperationException("Malformed identity token.");
        }

        return new TokenParts(
            0,
            firstDot,
            firstDot + 1,
            secondDot - firstDot - 1,
            secondDot + 1,
            token.Length - secondDot - 1
        );
    }

    private static string ResolveToken(JsonElement root)
    {
        if (root.TryGetProperty("Token", out JsonElement tokenUpper) && tokenUpper.ValueKind == JsonValueKind.String)
        {
            return tokenUpper.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("token", out JsonElement tokenLower) && tokenLower.ValueKind == JsonValueKind.String)
        {
            return tokenLower.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("AuthorizationToken", out JsonElement authUpper) && authUpper.ValueKind == JsonValueKind.String)
        {
            return authUpper.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("authorizationToken", out JsonElement authLower) && authLower.ValueKind == JsonValueKind.String)
        {
            return authLower.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("chain", out JsonElement chain) && chain.ValueKind == JsonValueKind.Array)
        {
            string last = string.Empty;
            foreach (JsonElement item in chain.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    last = item.GetString() ?? string.Empty;
                }
            }

            return last;
        }

        if (root.TryGetProperty("Chain", out JsonElement chainUpper) && chainUpper.ValueKind == JsonValueKind.Array)
        {
            string last = string.Empty;
            foreach (JsonElement item in chainUpper.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    last = item.GetString() ?? string.Empty;
                }
            }

            return last;
        }

        return string.Empty;
    }

    private static string GetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static long GetInt64(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
        {
            return number;
        }

        return 0;
    }

    private static byte[] DecodeBase64Url(ReadOnlySpan<char> value)
    {
        int padding = (4 - (value.Length & 3)) & 3;
        int charCount = value.Length + padding;
        char[] rentedChars = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                rentedChars[i] = c switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => c
                };
            }

            for (int i = 0; i < padding; i++)
            {
                rentedChars[value.Length + i] = '=';
            }

            int maxBytes = (charCount >> 2) * 3;
            byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(maxBytes);
            try
            {
                if (!Convert.TryFromBase64Chars(rentedChars.AsSpan(0, charCount), rentedBytes, out int written))
                {
                    throw new InvalidOperationException("Invalid base64url data.");
                }

                byte[] result = new byte[written];
                Buffer.BlockCopy(rentedBytes, 0, result, 0, written);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentedChars, clearArray: true);
        }
    }

    private readonly record struct TokenParts(
        int HeaderStart,
        int HeaderLength,
        int PayloadStart,
        int PayloadLength,
        int SignatureStart,
        int SignatureLength
    );

    private sealed class AuthConfig
    {
        public AuthConfig(string issuer, HashSet<string> algorithms, Dictionary<string, RSA> keys, DateTimeOffset expiresAt)
        {
            Issuer = issuer;
            Algorithms = algorithms;
            Keys = keys;
            ExpiresAt = expiresAt;
        }

        public string Issuer { get; }
        public HashSet<string> Algorithms { get; }
        public Dictionary<string, RSA> Keys { get; }
        public DateTimeOffset ExpiresAt { get; }
    }
}
