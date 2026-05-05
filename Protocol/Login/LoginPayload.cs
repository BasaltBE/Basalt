using System.Buffers;
using System.Text.Json;
using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Login;

public readonly record struct ClientData(
    string DeviceModel,
    string DeviceId,
    DeviceOS DeviceOs,
    long ClientRandomId,
    bool CompatibleWithClientSideChunkGen,
    int CurrentInputMode,
    int DefaultInputMode,
    string GameVersion,
    int GuiScale,
    bool IsEditorMode,
    string LanguageCode,
    int MaxViewDistance,
    int MemoryTier,
    string SkinId,
    string PlayFabId,
    string PlatformOfflineId,
    string PlatformOnlineId,
    int PlatformType,
    string SelfSignedId,
    string ServerAddress,
    string SkinResourcePatch,
    uint SkinImageWidth,
    uint SkinImageHeight,
    string SkinData,
    SkinAnimation[] AnimatedImageData,
    uint CapeImageWidth,
    uint CapeImageHeight,
    string CapeData,
    string SkinGeometryData,
    string SkinGeometryDataEngineVersion,
    string SkinAnimationData,
    string CapeId,
    string ArmSize,
    string SkinColor,
    string ThirdPartyName,
    bool ThirdPartyNameOnly,
    PersonaPiece[] PersonaPieces,
    TintPiece[] PieceTintColors,
    bool PremiumSkin,
    bool PersonaSkin,
    bool CapeOnClassicSkin,
    bool TrustedSkin,
    bool OverrideSkin,
    int UiProfile
);

public readonly record struct SkinAnimation(uint ImageWidth, uint ImageHeight, string Image, uint Type, float Frames, uint AnimationExpression);

public readonly record struct PersonaPiece(string PieceId, string PieceType, string PackId, bool IsDefault, string ProductId);

public readonly record struct TintPiece(string PieceType, string[] Colors);

public static class LoginPayload
{
    public static ClientData Parse(string clientJwt)
    {
        TokenParts parts = ParseTokenParts(clientJwt);

        byte[] payloadBytes = DecodeBase64Url(clientJwt.AsSpan(parts.PayloadStart, parts.PayloadLength));
        try
        {
            using JsonDocument payloadDoc = JsonDocument.Parse(payloadBytes);
            JsonElement payload = payloadDoc.RootElement;

            return new ClientData(
                GetString(payload, "DeviceModel"),
                GetString(payload, "DeviceId"),
                GetDeviceOs(payload),
                GetInt64(payload, "ClientRandomId"),
                GetBool(payload, "CompatibleWithClientSideChunkGen"),
                GetInt32(payload, "CurrentInputMode"),
                GetInt32(payload, "DefaultInputMode"),
                GetString(payload, "GameVersion"),
                GetInt32(payload, "GuiScale"),
                GetBool(payload, "IsEditorMode"),
                GetString(payload, "LanguageCode"),
                GetInt32(payload, "MaxViewDistance"),
                GetInt32(payload, "MemoryTier"),
                GetString(payload, "SkinId"),
                GetString(payload, "PlayFabId"),
                GetString(payload, "PlatformOfflineId"),
                GetString(payload, "PlatformOnlineId"),
                GetPlatformType(payload),
                GetString(payload, "SelfSignedId"),
                GetString(payload, "ServerAddress"),
                GetString(payload, "SkinResourcePatch"),
                GetUInt32(payload, "SkinImageWidth"),
                GetUInt32(payload, "SkinImageHeight"),
                GetString(payload, "SkinData"),
                GetAnimations(payload, "AnimatedImageData"),
                GetUInt32(payload, "CapeImageWidth"),
                GetUInt32(payload, "CapeImageHeight"),
                GetString(payload, "CapeData"),
                GetString(payload, "SkinGeometryData"),
                GetString(payload, "SkinGeometryDataEngineVersion"),
                GetString(payload, "SkinAnimationData"),
                GetString(payload, "CapeId"),
                GetString(payload, "ArmSize"),
                GetString(payload, "SkinColor"),
                GetString(payload, "ThirdPartyName"),
                GetBool(payload, "ThirdPartyNameOnly"),
                GetPersonaPieces(payload, "PersonaPieces"),
                GetTintPieces(payload, "PieceTintColors"),
                GetBool(payload, "PremiumSkin"),
                GetBool(payload, "PersonaSkin"),
                GetBool(payload, "CapeOnClassicSkin"),
                GetBool(payload, "TrustedSkin"),
                GetBool(payload, "OverrideSkin"),
                GetInt32(payload, "UIProfile")
            );
        }
        finally
        {
            Array.Clear(payloadBytes);
        }
    }

    private static SkinAnimation[] GetAnimations(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = value.GetArrayLength();
        if (count == 0)
        {
            return [];
        }

        SkinAnimation[] result = new SkinAnimation[count];
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result[index++] = new SkinAnimation(
                GetUInt32(item, "ImageWidth"),
                GetUInt32(item, "ImageHeight"),
                GetString(item, "Image"),
                GetUInt32(item, "Type"),
                GetFloat(item, "Frames"),
                GetUInt32(item, "AnimationExpression")
            );
        }

        return result;
    }

    private static PersonaPiece[] GetPersonaPieces(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = value.GetArrayLength();
        if (count == 0)
        {
            return [];
        }

        PersonaPiece[] result = new PersonaPiece[count];
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result[index++] = new PersonaPiece(
                GetString(item, "PieceId"),
                GetString(item, "PieceType"),
                GetString(item, "PackId"),
                GetBool(item, "IsDefault"),
                GetString(item, "ProductId")
            );
        }

        return result;
    }

    private static TintPiece[] GetTintPieces(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = value.GetArrayLength();
        if (count == 0)
        {
            return [];
        }

        TintPiece[] result = new TintPiece[count];
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            result[index++] = new TintPiece(GetString(item, "PieceType"), GetColors(item));
        }

        return result;
    }

    private static string[] GetColors(JsonElement element)
    {
        if (!element.TryGetProperty("Colors", out JsonElement colorsElement) || colorsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        int count = colorsElement.GetArrayLength();
        if (count == 0)
        {
            return [];
        }

        string[] colors = new string[count];
        int index = 0;
        foreach (JsonElement color in colorsElement.EnumerateArray())
        {
            colors[index++] = color.ValueKind == JsonValueKind.String ? color.GetString() ?? string.Empty : string.Empty;
        }

        return colors;
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

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number) ? number : 0;
    }

    private static DeviceOS GetDeviceOs(JsonElement payload)
    {
        long raw = GetInt64(payload, "DeviceOS");
        if (raw < (long)DeviceOS.Undefined || raw > (long)DeviceOS.Linux)
        {
            return DeviceOS.Undefined;
        }

        return (DeviceOS)raw;
    }

    private static int GetInt32(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : 0;
    }

    private static uint GetUInt32(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetUInt32(out uint number) ? number : 0;
    }

    private static bool GetBool(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();
    }

    private static float GetFloat(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return 0f;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float number) ? number : 0f;
    }

    private static int GetPlatformType(JsonElement payload)
    {
        int platformType = GetInt32(payload, "PlatformType");
        if (platformType != 0)
        {
            return platformType;
        }

        return GetInt32(payload, "PlayformType");
    }

    private static TokenParts ParseTokenParts(string token)
    {
        int firstDot = token.IndexOf('.');
        if (firstDot <= 0)
        {
            throw new InvalidOperationException("Malformed client token.");
        }

        int secondDot = token.IndexOf('.', firstDot + 1);
        if (secondDot <= firstDot + 1 || secondDot == token.Length - 1)
        {
            throw new InvalidOperationException("Malformed client token.");
        }

        if (token.IndexOf('.', secondDot + 1) >= 0)
        {
            throw new InvalidOperationException("Malformed client token.");
        }

        return new TokenParts(firstDot + 1, secondDot - firstDot - 1);
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

    private readonly record struct TokenParts(int PayloadStart, int PayloadLength);
}

