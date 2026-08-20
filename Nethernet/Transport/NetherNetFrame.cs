using System.Buffers;

namespace Basalt.Core.Nethernet;

public static class NetherNetFrame {
    public const int HeaderSize = 1;

    public static void Send(
        ReadOnlySpan<byte> payload,
        int maximumPayloadSize,
        Action<byte[], int> send) {
        ArgumentNullException.ThrowIfNull(send);
        if (maximumPayloadSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maximumPayloadSize));
        }

        int fragmentCount = payload.Length == 0
            ? 1
            : checked((payload.Length + maximumPayloadSize - 1) / maximumPayloadSize);
        if (fragmentCount > byte.MaxValue + 1) {
            throw new ArgumentOutOfRangeException(nameof(payload), "The payload has too many fragments.");
        }

        for (int index = 0; index < fragmentCount; index++) {
            int offset = index * maximumPayloadSize;
            int length = Math.Min(maximumPayloadSize, payload.Length - offset);
            byte[] frame = ArrayPool<byte>.Shared.Rent(length + HeaderSize);
            frame[0] = checked((byte)(fragmentCount - index - 1));
            payload.Slice(offset, length).CopyTo(frame.AsSpan(HeaderSize));
            try {
                send(frame, length + HeaderSize);
            }
            finally {
                ArrayPool<byte>.Shared.Return(frame);
            }
        }
    }
}
