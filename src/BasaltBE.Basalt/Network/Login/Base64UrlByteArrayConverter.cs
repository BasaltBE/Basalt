using System.Text.Json;
using System.Text.Json.Serialization;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Network.Login;

internal sealed class Base64UrlByteArrayConverter : JsonConverter<byte[]> {
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        if (reader.ValueIsEscaped || reader.HasValueSequence) return Base64Url.Decode(reader.GetString()!.AsSpan());

        ReadOnlySpan<byte> encoded = reader.ValueSpan;
        if (encoded.Length % 4 == 0 && encoded.IndexOfAny((byte)'-', (byte)'_') < 0) return reader.GetBytesFromBase64();
        return Base64Url.Decode(encoded);
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) =>
        writer.WriteBase64StringValue(value);
}
