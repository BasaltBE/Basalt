using System.Buffers;
using System.Text.Json;

namespace Basalt.Protocol.Login;

internal static class JsonValue
{
    public static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value)
            ? value.GetString() ?? string.Empty
            : string.Empty;

    public static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        value.GetBoolean();

    public static long GetInt64(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetInt64(out long number) ? number : 0;

    public static int GetInt32(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int number) ? number : 0;

    public static uint GetUInt32(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetUInt32(out uint number) ? number : 0;

    public static float GetFloat(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetSingle(out float number) ? number : 0f;

    public static IEnumerable<JsonElement> GetArray(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : [];

    public static byte[] DecodeBase64Url(ReadOnlySpan<char> value)
    {
        int padding = (4 - (value.Length & 3)) & 3;
        int charCount = value.Length + padding;

        char[] rentedChars = ArrayPool<char>.Shared.Rent(charCount);
        byte[] rentedBytes = ArrayPool<byte>.Shared.Rent((charCount >> 2) * 3);
        try
        {
            value.CopyTo(rentedChars);
            rentedChars.AsSpan(0, value.Length).Replace('-', '+');
            rentedChars.AsSpan(0, value.Length).Replace('_', '/');
            rentedChars.AsSpan(value.Length, padding).Fill('=');

            if (!Convert.TryFromBase64Chars(rentedChars.AsSpan(0, charCount), rentedBytes, out int written))
                throw new InvalidOperationException("Invalid base64url data.");

            return rentedBytes.AsSpan(0, written).ToArray();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentedChars, clearArray: true);
            ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
        }
    }
}