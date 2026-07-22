using System.Buffers;
using System.Text.Json;
using Basalt.Protocol.Login.Data;
using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Login;


public static class LoginPayload {
    public static ClientData Parse(string clientJwt) {
        TokenParts parts = ParseTokenParts(clientJwt);

        byte[] payloadBytes = JsonValue.DecodeBase64Url(clientJwt.AsSpan(parts.PayloadStart, parts.PayloadLength));
        using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);
        JsonElement payload = payloadDoc.RootElement;

        return new ClientData(
            JsonValue.GetString(payload, "DeviceModel"),
            JsonValue.GetString(payload, "DeviceId"),
            GetDeviceOs(payload),
            JsonValue.GetInt64(payload, "ClientRandomId"),
            JsonValue.GetBool(payload, "CompatibleWithClientSideChunkGen"),
            JsonValue.GetInt32(payload, "CurrentInputMode"),
            JsonValue.GetInt32(payload, "DefaultInputMode"),
            JsonValue.GetString(payload, "GameVersion"),
            JsonValue.GetInt32(payload, "GuiScale"),
            JsonValue.GetBool(payload, "IsEditorMode"),
            JsonValue.GetString(payload, "LanguageCode"),
            JsonValue.GetInt32(payload, "MaxViewDistance"),
            JsonValue.GetInt32(payload, "MemoryTier"),
            JsonValue.GetString(payload, "SkinId"),
            JsonValue.GetString(payload, "PlayFabId"),
            JsonValue.GetString(payload, "PlatformOfflineId"),
            JsonValue.GetString(payload, "PlatformOnlineId"),
            GetPlatformType(payload),
            JsonValue.GetString(payload, "SelfSignedId"),
            JsonValue.GetString(payload, "ServerAddress"),
            JsonValue.GetString(payload, "SkinResourcePatch"),
            JsonValue.GetUInt32(payload, "SkinImageWidth"),
            JsonValue.GetUInt32(payload, "SkinImageHeight"),
            JsonValue.GetString(payload, "SkinData"),
            GetAnimations(payload, "AnimatedImageData"),
            JsonValue.GetUInt32(payload, "CapeImageWidth"),
            JsonValue.GetUInt32(payload, "CapeImageHeight"),
            JsonValue.GetString(payload, "CapeData"),
            JsonValue.GetString(payload, "SkinGeometryData"),
            JsonValue.GetString(payload, "SkinGeometryDataEngineVersion"),
            JsonValue.GetString(payload, "SkinAnimationData"),
            JsonValue.GetString(payload, "CapeId"),
            JsonValue.GetString(payload, "ArmSize"),
            JsonValue.GetString(payload, "SkinColor"),
            JsonValue.GetString(payload, "ThirdPartyName"),
            JsonValue.GetBool(payload, "ThirdPartyNameOnly"),
            GetPersonaPieces(payload, "PersonaPieces"),
            GetTintPieces(payload, "PieceTintColors"),
            JsonValue.GetBool(payload, "PremiumSkin"),
            JsonValue.GetBool(payload, "PersonaSkin"),
            JsonValue.GetBool(payload, "CapeOnClassicSkin"),
            JsonValue.GetBool(payload, "TrustedSkin"),
            JsonValue.GetBool(payload, "OverrideSkin"),
            JsonValue.GetInt32(payload, "UIProfile")
        );
    }

    private static DeviceOS GetDeviceOs(JsonElement payload) =>
      Enum.IsDefined((DeviceOS)JsonValue.GetInt64(payload, "DeviceOS"))
          ? (DeviceOS)JsonValue.GetInt64(payload, "DeviceOS")
          : DeviceOS.Undefined;

    private static int GetPlatformType(JsonElement payload) =>
        JsonValue.GetInt32(payload, "PlatformType") is int p and not 0 ? p : JsonValue.GetInt32(payload, "PlayformType");

    private static SkinAnimation[] GetAnimations(JsonElement element, string name) =>
        JsonValue.GetArray(element, name).Select(item => new SkinAnimation(
            JsonValue.GetUInt32(item, "ImageWidth"),
            JsonValue.GetUInt32(item, "ImageHeight"),
            JsonValue.GetString(item, "Image"),
            JsonValue.GetUInt32(item, "Type"),
            JsonValue.GetFloat(item, "Frames"),
            JsonValue.GetUInt32(item, "AnimationExpression")
        )).ToArray();

    private static PersonaPiece[] GetPersonaPieces(JsonElement element, string name) =>
        JsonValue.GetArray(element, name).Select(item => new PersonaPiece(
            JsonValue.GetString(item, "PieceId"),
            JsonValue.GetString(item, "PieceType"),
            JsonValue.GetString(item, "PackId"),
            JsonValue.GetBool(item, "IsDefault"),
            JsonValue.GetString(item, "ProductId")
        )).ToArray();

    private static string[] GetColors(JsonElement element) =>
        JsonValue.GetArray(element, "Colors")
            .Select(c => c.GetString() ?? string.Empty)
            .ToArray();

    private static TintPiece[] GetTintPieces(JsonElement element, string name) =>
        JsonValue.GetArray(element, name)
            .Select(item => new TintPiece(JsonValue.GetString(item, "PieceType"), GetColors(item)))
            .ToArray();

    private static TokenParts ParseTokenParts(string token) {
        int firstDot = token.IndexOf('.');
        int secondDot = firstDot > 0 ? token.IndexOf('.', firstDot + 1) : -1;

        if (firstDot <= 0
            || secondDot <= firstDot + 1
            || secondDot == token.Length - 1
            || token.IndexOf('.', secondDot + 1) >= 0) {
            throw new InvalidOperationException("Malformed client token.");
        }

        return new TokenParts(firstDot + 1, secondDot - firstDot - 1);
    }

    private readonly record struct TokenParts(int PayloadStart, int PayloadLength);
}
