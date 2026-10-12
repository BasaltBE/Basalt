using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Network.Login;

internal sealed class LoginAuthenticator : IDisposable {
    private const string Issuer = "https://authorization.franchise.minecraft-services.net/";
    private const string Audience = "api://auth-minecraft-services/multiplayer";
    private const string MetadataUrl = Issuer + ".well-known/openid-configuration";
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly TimeSpan KeyLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan KeyRefreshInterval = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan KeyRetryInterval = TimeSpan.FromMinutes(1);
    private readonly Func<CancellationToken, OpenIdSigningKey[]> fetchSigningKeys;
    private readonly TimeSpan refreshInterval;
    private readonly AutoResetEvent refreshSignal = new(false);
    private readonly ManualResetEventSlim initialRefresh = new(false);
    private readonly CancellationTokenSource refreshCancellation = new();
    private readonly Thread refreshThread;
    private SigningKeyCache? signingKeyCache;
    private Exception? initialRefreshError;
    private long lastRefreshRequest = Environment.TickCount64 - (long)KeyRetryInterval.TotalMilliseconds;
    private int started;
    private int disposed;

    public LoginAuthenticator() : this(FetchSigningKeys, KeyRefreshInterval) { }

    internal LoginAuthenticator(Func<CancellationToken, OpenIdSigningKey[]> fetchSigningKeys, TimeSpan refreshInterval) {
        this.fetchSigningKeys = fetchSigningKeys;
        this.refreshInterval = refreshInterval;
        refreshThread = new Thread(RefreshSigningKeys) {
            IsBackground = true,
            Name = "BasaltBE Login Key Refresh"
        };
    }

    public void Start() {
        using ProfilerZone zone = Profiler.BeginZone("Login.Start");
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.Exchange(ref started, 1) == 0) refreshThread.Start();
        initialRefresh.Wait();
        if (initialRefreshError is not null) {
            Exception error = initialRefreshError;
            Dispose();
            throw new InvalidOperationException("Minecraft authorization signing keys could not be loaded.", error);
        }
    }

    public void Dispose() {
        using ProfilerZone zone = Profiler.BeginZone("Login.Dispose");
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        refreshCancellation.Cancel();
        refreshSignal.Set();
        if (Volatile.Read(ref started) != 0) refreshThread.Join();
        refreshCancellation.Dispose();
        refreshSignal.Dispose();
        initialRefresh.Dispose();
    }

    public AuthenticatedLogin Authenticate(ReadOnlyMemory<byte> connectionRequest) {
        using ProfilerZone authenticationZone = Profiler.BeginZone("Login.Authenticate");
        LoginTokenData tokens;
        using (Profiler.BeginZone("Login.ParseConnectionRequest"))
            tokens = LoginTokenParser.Parse(connectionRequest);
        LoginIdentity identity = tokens.Identity;
        if (identity.AuthenticationType != 0 || string.IsNullOrEmpty(identity.Token))
            throw new CryptographicException("Login has no Xbox authenticated token.");

        TokenParts identityParts;
        MultiplayerTokenHeader identityHeader;
        MultiplayerTokenPayload claims;
        using (Profiler.BeginZone("Login.ParseMultiplayerToken")) {
            identityParts = ParseToken(Encoding.UTF8.GetBytes(identity.Token));
            identityHeader = JsonSerializer.Deserialize<MultiplayerTokenHeader>(
                identityParts.Header, LoginJson.Options);
            claims = JsonSerializer.Deserialize<MultiplayerTokenPayload>(
                identityParts.Payload, LoginJson.Options);
        }
        ValidateMultiplayerToken(identityParts, identityHeader, claims);

        ClientTokenData clientTokenData = tokens.Client;
        ECParameters clientKey = clientTokenData.PublicKey;

        ClientData client;
        Guid uuid;
        byte[] skin;
        using (Profiler.BeginZone("Login.ValidateClientData")) {
            ECParameters authenticatedKey = ReadPublicKey(claims.ClientPublicKey);
            if (!clientKey.Q.X!.AsSpan().SequenceEqual(authenticatedKey.Q.X) ||
                !clientKey.Q.Y!.AsSpan().SequenceEqual(authenticatedKey.Q.Y))
                throw new CryptographicException("Client data key does not match the authenticated multiplayer token.");

            client = clientTokenData.Client;
            uuid = ParsePlayerUuid(client.SelfSignedId);
            skin = string.IsNullOrEmpty(client.SkinData) ? [] : Base64Url.Decode(client.SkinData.AsSpan());
            if (skin.Length > 2 * 1024 * 1024)
                throw new InvalidDataException("Bedrock skin data exceeds the size limit.");
        }

        return new AuthenticatedLogin(
            claims.Username,
            claims.Xuid,
            uuid,
            client.DeviceOS,
            client.DeviceModel ?? string.Empty,
            skin
        );
    }

    private void ValidateMultiplayerToken(
        TokenParts token,
        MultiplayerTokenHeader header,
        MultiplayerTokenPayload claims
    ) {
        using ProfilerZone zone = Profiler.BeginZone("Login.ValidateMultiplayerToken");
        if (header.Algorithm is not ("RS256" or "ES384") || string.IsNullOrEmpty(header.KeyId))
            throw new CryptographicException("Xbox multiplayer token header is invalid.");

        OpenIdSigningKey key = GetSigningKey(header.KeyId, header.Algorithm);
        bool validSignature = header.Algorithm switch {
            "RS256" when key.KeyType == "RSA" => VerifyRsa(token, key),
            "ES384" when key.KeyType == "EC" => VerifyEc(token, key),
            _ => false
        };
        if (!validSignature)
            throw new CryptographicException("Xbox multiplayer token signature is invalid.");

        bool validAudience = claims.Audience.ValueKind == JsonValueKind.String
            ? claims.Audience.GetString() == Audience
            : claims.Audience.ValueKind == JsonValueKind.Array && claims.Audience.EnumerateArray()
                .Any(value => value.ValueKind == JsonValueKind.String && value.GetString() == Audience);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool validIssuer = claims.Issuer?.TrimEnd('/') == Issuer.TrimEnd('/');
        bool validLifetime = claims.ExpiresAt > now && claims.NotBefore <= now + 120;
        bool hasPlayerIdentity =
            !string.IsNullOrEmpty(claims.Xuid) && !string.IsNullOrEmpty(claims.Username);
        if (!validIssuer || !validAudience || !validLifetime || !hasPlayerIdentity)
            throw new CryptographicException("Xbox multiplayer token claims are invalid or expired.");
    }

    internal static Guid ParsePlayerUuid(string? selfSignedId) =>
        Guid.TryParse(selfSignedId, out Guid uuid) ? uuid : Guid.Empty;

    internal OpenIdSigningKey GetSigningKey(string keyId, string algorithm) {
        using ProfilerZone zone = Profiler.BeginZone("Login.GetSigningKey");
        SigningKeyCache? cache = Volatile.Read(ref signingKeyCache);
        if (cache is not null && DateTimeOffset.UtcNow < cache.Expires) {
            OpenIdSigningKey? key = FindSigningKey(cache.Keys, keyId, algorithm);
            if (key is not null) return key.Value;
        }

        RequestSigningKeyRefresh();
        throw new CryptographicException("Xbox multiplayer token signing key is not trusted or the key cache has expired.");
    }

    private void RefreshSigningKeys() {
        bool firstRefresh = true;
        while (Volatile.Read(ref disposed) == 0) {
            Exception? error = null;
            try {
                OpenIdSigningKey[] keys;
                using (Profiler.BeginZone("Login.RefreshSigningKeys"))
                    keys = fetchSigningKeys(refreshCancellation.Token);
                if (keys.Length == 0)
                    throw new CryptographicException("Minecraft authorization service returned no signing keys.");
                DateTimeOffset expires = DateTimeOffset.UtcNow.Add(KeyLifetime);
                Volatile.Write(ref signingKeyCache, new SigningKeyCache(keys, expires));
            }
            catch (Exception refreshError) {
                error = refreshError;
                if (firstRefresh) initialRefreshError = refreshError;
                else Logger.Scoped("Network").Warn($"Could not refresh Minecraft authorization signing keys: {refreshError.Message}");
            }

            if (firstRefresh) {
                initialRefresh.Set();
                if (error is not null) return;
                firstRefresh = false;
            }

            refreshSignal.WaitOne(error is null ? refreshInterval : KeyRetryInterval);
        }
    }

    private void RequestSigningKeyRefresh() {
        long now = Environment.TickCount64;
        long previous = Interlocked.Read(ref lastRefreshRequest);
        if (now - previous < KeyRetryInterval.TotalMilliseconds ||
            Interlocked.CompareExchange(ref lastRefreshRequest, now, previous) != previous)
            return;
        refreshSignal.Set();
    }

    private static OpenIdSigningKey[] FetchSigningKeys(CancellationToken cancellationToken) {
        using HttpRequestMessage metadataRequest = new(HttpMethod.Get, MetadataUrl);
        using HttpResponseMessage metadataResponse = httpClient.Send(metadataRequest, cancellationToken);
        metadataResponse.EnsureSuccessStatusCode();
        using Stream metadataStream = metadataResponse.Content.ReadAsStream(cancellationToken);
        OpenIdMetadata metadata = JsonSerializer.Deserialize<OpenIdMetadata>(metadataStream, LoginJson.Options);
        if (metadata.Issuer?.TrimEnd('/') != Issuer.TrimEnd('/') ||
            !Uri.TryCreate(metadata.KeysUrl, UriKind.Absolute, out Uri? keysUri) ||
            keysUri.Scheme != Uri.UriSchemeHttps || keysUri.Host != "authorization.franchise.minecraft-services.net")
            throw new InvalidDataException("Minecraft authorization signing key URL is not trusted.");

        using HttpRequestMessage keysRequest = new(HttpMethod.Get, keysUri);
        using HttpResponseMessage keysResponse = httpClient.Send(keysRequest, cancellationToken);
        keysResponse.EnsureSuccessStatusCode();
        using Stream keysStream = keysResponse.Content.ReadAsStream(cancellationToken);
        OpenIdKeySet keySet = JsonSerializer.Deserialize<OpenIdKeySet>(keysStream, LoginJson.Options);
        return keySet.Keys ?? [];
    }

    private static OpenIdSigningKey? FindSigningKey(OpenIdSigningKey[] keys, string keyId, string algorithm) {
        foreach (OpenIdSigningKey key in keys)
            if (key.KeyId == keyId && (string.IsNullOrEmpty(key.Algorithm) || key.Algorithm == algorithm))
                return key;
        return null;
    }

    private static bool VerifyRsa(TokenParts token, OpenIdSigningKey key) {
        using RSA rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters {
            Modulus = Base64Url.Decode(key.Modulus.AsSpan()),
            Exponent = Base64Url.Decode(key.Exponent.AsSpan())
        });
        return rsa.VerifyData(token.SigningInput, token.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyEc(TokenParts token, OpenIdSigningKey key) {
        byte[] x = Base64Url.Decode(key.X.AsSpan());
        byte[] y = Base64Url.Decode(key.Y.AsSpan());
        if (x.Length != 48 || y.Length != 48)
            throw new CryptographicException("Xbox multiplayer signing key is not P-384.");
        using ECDsa ecdsa = ECDsa.Create(new ECParameters {
            Curve = ECCurve.NamedCurves.nistP384,
            Q = new ECPoint { X = x, Y = y }
        });
        return ecdsa.VerifyData(token.SigningInput, token.Signature,
            HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static ECParameters ReadPublicKey(JsonElement key) {
        if (key.ValueKind == JsonValueKind.String)
            return ReadPublicKey(key.GetString()!);
        if (key.ValueKind != JsonValueKind.Object || key.GetProperty("kty").GetString() != "EC" ||
            key.GetProperty("crv").GetString() != "P-384")
            throw new CryptographicException("Authenticated client public key is invalid.");

        byte[] x = Base64Url.Decode(key.GetProperty("x").GetString()!.AsSpan());
        byte[] y = Base64Url.Decode(key.GetProperty("y").GetString()!.AsSpan());
        if (x.Length != 48 || y.Length != 48)
            throw new CryptographicException("Authenticated client public key coordinates are invalid.");
        return new ECParameters { Curve = ECCurve.NamedCurves.nistP384, Q = new ECPoint { X = x, Y = y } };
    }

    private static ECParameters ReadPublicKey(string encodedKey) {
        byte[] keyBytes = Base64Url.Decode(encodedKey.AsSpan());
        using ECDsa key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(keyBytes, out int bytesRead);
        if (bytesRead != keyBytes.Length || key.KeySize != 384)
            throw new CryptographicException("Authenticated client public key must use P-384.");
        return key.ExportParameters(false);
    }

    private static TokenParts ParseToken(ReadOnlySpan<byte> token) {
        int firstDot = token.IndexOf((byte)'.');
        int secondDotOffset = firstDot < 0 ? -1 : token[(firstDot + 1)..].IndexOf((byte)'.');
        int secondDot = secondDotOffset < 0 ? -1 : firstDot + secondDotOffset + 1;
        if (firstDot <= 0 || secondDot <= firstDot + 1 || secondDot >= token.Length - 1)
            throw new InvalidDataException("Bedrock login token is invalid.");

        byte[] header = Base64Url.Decode(token[..firstDot]);
        byte[] payload = Base64Url.Decode(token[(firstDot + 1)..secondDot]);
        byte[] signature = Base64Url.Decode(token[(secondDot + 1)..]);
        byte[] signingInput = token[..secondDot].ToArray();
        return new TokenParts(header, payload, signature, signingInput);
    }

    private readonly record struct TokenParts(byte[] Header, byte[] Payload, byte[] Signature, byte[] SigningInput);
    private sealed record SigningKeyCache(OpenIdSigningKey[] Keys, DateTimeOffset Expires);
}
