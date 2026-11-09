using System.Security.Cryptography;
using System.Text.Json;
using BasaltBE.Client.Auth;

namespace BasaltBE.Client.Login;

internal sealed class BedrockAuthenticator(HttpClient httpClient) {
    private readonly MicrosoftAuthenticator microsoft = new(httpClient);
    private readonly XboxAuthenticator xbox = new(httpClient);
    private readonly MinecraftAuthenticator minecraft = new(httpClient);

    internal async Task<BedrockAuthentication> AuthenticateAsync(
        string cachePath,
        Uri serverUri,
        Action<MicrosoftDeviceCode> showCode,
        CancellationToken cancellationToken
    ) {
        BedrockSkinData skin = await ReadSkinAsync(cancellationToken).ConfigureAwait(false);
        AuthCache? cache = await AuthCacheFile.LoadAsync(cachePath, cancellationToken).ConfigureAwait(false);
        string privateKey;
        MicrosoftToken microsoftToken;
        if (cache is null) {
            microsoftToken = await microsoft.AuthenticateAsync(showCode, cancellationToken).ConfigureAwait(false);
            privateKey = GeneratePrivateKey();
        }
        else {
            privateKey = cache.PrivateKey;
            try {
                microsoftToken = await microsoft.RefreshAsync(
                    cache.RefreshToken,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (MicrosoftTokenException error) when (error.Code == "invalid_grant") {
                microsoftToken = await microsoft.AuthenticateAsync(showCode, cancellationToken).ConfigureAwait(false);
                privateKey = GeneratePrivateKey();
            }
        }

        await AuthCacheFile.SaveAsync(
            cachePath,
            new AuthCache(microsoftToken.RefreshToken, privateKey),
            cancellationToken
        ).ConfigureAwait(false);
        using ECDsa key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
        string publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

        XboxAuthentication xboxAuthentication = await xbox.AuthenticateAsync(
            microsoftToken.AccessToken,
            cancellationToken
        ).ConfigureAwait(false);
        string multiplayerToken = await minecraft.AuthenticateAsync(
            xboxAuthentication.PlayFabAuthorization,
            publicKey,
            cancellationToken
        ).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(multiplayerToken))
            throw new InvalidDataException("Minecraft authentication did not return Bedrock login tokens.");

        BedrockAccount account = BedrockClientToken.ReadAccount(multiplayerToken, xboxAuthentication.BedrockAccount);
        string clientToken = BedrockClientToken.Create(key, skin, account, serverUri);
        return new BedrockAuthentication(account, multiplayerToken, clientToken, privateKey);
    }

    private static async Task<BedrockSkinData> ReadSkinAsync(CancellationToken cancellationToken) {
        await using Stream skinStream = typeof(BedrockAuthenticator).Assembly
            .GetManifestResourceStream("BasaltBE.Client.Data.skin.json")
            ?? throw new InvalidOperationException("Embedded skin data is missing.");
        BedrockSkinData skin = await JsonSerializer.DeserializeAsync<BedrockSkinData>(
            skinStream,
            BedrockClientToken.SkinDataJsonOptions,
            cancellationToken
        ).ConfigureAwait(false)
            ?? throw new InvalidDataException("Skin data file is empty.");
        if (string.IsNullOrWhiteSpace(skin.SkinData))
            throw new InvalidDataException("Skin data file does not contain SkinData.");
        return skin;
    }

    private static string GeneratePrivateKey() {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        return Convert.ToBase64String(key.ExportPkcs8PrivateKey());
    }
}

internal sealed record BedrockAuthentication(
    BedrockAccount Account,
    string IdentityToken,
    string ClientToken,
    string PrivateKey
);
