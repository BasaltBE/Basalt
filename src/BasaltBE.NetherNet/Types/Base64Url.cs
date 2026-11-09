using System.Buffers;
using System.Buffers.Text;

namespace BasaltBE.NetherNet.Types;

public static class Base64Url {
    public static string Encode(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(ReadOnlySpan<char> value) {
        int padding = value.Length != 0 && value[^1] == '='
            ? value.Length > 1 && value[^2] == '=' ? 2 : 1
            : 0;
        int unpaddedLength = value.Length - padding;
        int remainder = unpaddedLength & 3;
        int requiredPadding = (4 - remainder) & 3;
        if (remainder == 1 || padding != 0 && padding != requiredPadding) throw new InvalidDataException("Base64Url data is invalid.");

        int paddedLength = checked((value.Length + requiredPadding - padding + 3) & ~3);
        int outputLength = paddedLength / 4 * 3 - (padding == 0 ? requiredPadding : padding);
        char[] characters = ArrayPool<char>.Shared.Rent(paddedLength);
        try {
            Span<char> encoded = characters.AsSpan(0, paddedLength);
            for (int index = 0; index < value.Length; index++)
                encoded[index] = value[index] switch {
                    '-' => '+',
                    '_' => '/',
                    char character when character <= 127 => character,
                    _ => throw new InvalidDataException("Base64Url data is invalid.")
                };
            encoded[value.Length..].Fill('=');

            byte[] output = GC.AllocateUninitializedArray<byte>(outputLength);
            if (!Convert.TryFromBase64Chars(encoded, output, out int written) || written != outputLength)
                throw new InvalidDataException("Base64Url data is invalid.");
            return output;
        } finally {
            ArrayPool<char>.Shared.Return(characters, clearArray: true);
        }
    }

    public static byte[] Decode(ReadOnlySpan<byte> value) {
        int outputLength = GetDecodedLength(value, out _, out _, out _);
        byte[] output = GC.AllocateUninitializedArray<byte>(outputLength);
        Decode(value, output);
        return output;
    }

    public static int Decode(ReadOnlySpan<byte> value, Span<byte> output) {
        int outputLength = GetDecodedLength(value, out int paddedLength, out int padding, out int requiredPadding);
        if (output.Length < outputLength) throw new ArgumentException("Output buffer is too small.", nameof(output));

        byte[] characters = ArrayPool<byte>.Shared.Rent(paddedLength);
        try {
            Span<byte> encoded = characters.AsSpan(0, paddedLength);
            for (int index = 0; index < value.Length; index++)
                encoded[index] = value[index] switch {
                    (byte)'-' => (byte)'+',
                    (byte)'_' => (byte)'/',
                    byte character when character <= 127 => character,
                    _ => throw new InvalidDataException("Base64Url data is invalid.")
                };
            encoded[value.Length..].Fill((byte)'=');

            OperationStatus status = Base64.DecodeFromUtf8(encoded, output[..outputLength], out int consumed, out int written);
            if (status != OperationStatus.Done || consumed != paddedLength || written != outputLength)
                throw new InvalidDataException("Base64Url data is invalid.");
            return written;
        } finally {
            ArrayPool<byte>.Shared.Return(characters, clearArray: true);
        }
    }

    private static int GetDecodedLength(
        ReadOnlySpan<byte> value,
        out int paddedLength,
        out int padding,
        out int requiredPadding
    ) {
        padding = value.Length != 0 && value[^1] == (byte)'='
            ? value.Length > 1 && value[^2] == (byte)'=' ? 2 : 1
            : 0;
        int unpaddedLength = value.Length - padding;
        int remainder = unpaddedLength & 3;
        requiredPadding = (4 - remainder) & 3;
        if (remainder == 1 || padding != 0 && padding != requiredPadding) throw new InvalidDataException("Base64Url data is invalid.");

        paddedLength = checked((value.Length + requiredPadding - padding + 3) & ~3);
        return paddedLength / 4 * 3 - (padding == 0 ? requiredPadding : padding);
    }
}
