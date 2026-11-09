using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BasaltBE.Client.Auth;

namespace BasaltBE.Client.Login;

internal static class BedrockClientToken {
    internal static readonly JsonSerializerOptions SkinDataJsonOptions = new() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    internal static string Create(
        ECDsa key,
        BedrockSkinData skin,
        BedrockAccount account,
        Uri serverUri
    ) {
        string header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new {
            alg = "ES384",
            x5u = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo())
        }));
        string deviceId = Guid.NewGuid().ToString();
        BedrockClientData clientData = new() {
            ClientRandomId = -Random.Shared.NextInt64(1, long.MaxValue),
            ClientEditorConnectionIntent = 2,
            ClientIsEditorCapable = true,
            CompatibleWithClientSideChunkGen = false,
            CurrentInputMode = 1,
            DefaultInputMode = 1,
            DeviceId = deviceId,
            DeviceModel = Environment.MachineName,
            DeviceOS = 8,
            FilterProfanity = true,
            GameVersion = Protocol.BedrockProtocol.MinecraftVersion,
            GraphicsMode = 1,
            GuiScale = 0,
            LanguageCode = "en_US",
            MaxViewDistance = 74,
            MemoryTier = 4,
            PlatformOfflineId = Guid.NewGuid().ToString("N"),
            PlatformOnlineId = Random.Shared.NextInt64(1_000_000_000_000_000, 10_000_000_000_000_000)
                .ToString(CultureInfo.InvariantCulture),
            PlatformType = 0,
            ProfileHash = string.Empty,
            SelfSignedId = Guid.NewGuid().ToString(),
            ServerAddress = $"{serverUri.ToString().TrimEnd('/')}:{serverUri.Port}",
            ThirdPartyName = account.Username,
            UIProfile = 0,
            AnimatedImageData = skin.AnimatedImageData,
            ArmSize = skin.ArmSize,
            CapeData = skin.CapeData,
            CapeId = skin.CapeId,
            CapeImageHeight = skin.CapeImageHeight,
            CapeImageWidth = skin.CapeImageWidth,
            CapeOnClassicSkin = skin.CapeOnClassicSkin,
            OverrideSkin = skin.OverrideSkin,
            PersonaPieces = skin.PersonaPieces,
            PersonaSkin = skin.PersonaSkin,
            PieceTintColors = skin.PieceTintColors,
            PremiumSkin = skin.PremiumSkin,
            SkinAnimationData = skin.SkinAnimationData,
            SkinColor = skin.SkinColor,
            SkinData = skin.SkinData,
            SkinGeometryData = skin.SkinGeometryData,
            SkinGeometryDataEngineVersion = skin.SkinGeometryDataEngineVersion,
            SkinId = skin.SkinId,
            SkinImageHeight = skin.SkinImageHeight,
            SkinImageWidth = skin.SkinImageWidth,
            SkinResourcePatch = skin.SkinResourcePatch,
            TrustedSkin = skin.TrustedSkin
        };
        string payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(
            clientData,
            SkinDataJsonOptions
        ));
        string unsignedToken = $"{header}.{payload}";
        byte[] signature = key.SignData(
            Encoding.ASCII.GetBytes(unsignedToken),
            HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation
        );
        return $"{unsignedToken}.{Base64Url(signature)}";
    }

    internal static BedrockAccount ReadAccount(string multiplayerToken, BedrockAccount fallback) {
        string[] parts = multiplayerToken.Split('.');
        if (parts.Length != 3) return fallback;
        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
        using JsonDocument document = JsonDocument.Parse(Convert.FromBase64String(payload));
        JsonElement claims = document.RootElement;
        string username = AuthenticationJson.GetString(claims, "xname");
        string xuid = AuthenticationJson.GetString(claims, "xid");
        return new BedrockAccount(
            string.IsNullOrWhiteSpace(username) ? fallback.Username : username,
            string.IsNullOrWhiteSpace(xuid) ? fallback.Xuid : xuid
        );
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
