using System.Buffers;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetReassembler : IDisposable {
    private byte[]? _payload;
    private int _length;
    private int _remainingFragments = -1;

    public bool Add(ReadOnlySpan<byte> frame, out byte[] payload) {
        payload = Array.Empty<byte>();
        if (frame.Length == 0) {
            return false;
        }

        int remainingFragments = frame[0];
        if (_remainingFragments < 0) {
            _remainingFragments = remainingFragments;
        }

        if (remainingFragments != _remainingFragments) {
            Reset();
            return false;
        }

        int payloadLength = frame.Length - NetherNetFrame.HeaderSize;
        EnsureCapacity(_length + payloadLength);
        frame[NetherNetFrame.HeaderSize..].CopyTo(_payload.AsSpan(_length));
        _length += payloadLength;
        if (remainingFragments != 0) {
            _remainingFragments--;
            return false;
        }

        payload = GC.AllocateUninitializedArray<byte>(_length);
        _payload.AsSpan(0, _length).CopyTo(payload);
        Reset();
        return true;
    }

    public void Dispose() {
        if (_payload is not null) {
            ArrayPool<byte>.Shared.Return(_payload);
            _payload = null;
        }
    }

    private void Reset() {
        _length = 0;
        _remainingFragments = -1;
    }

    private void EnsureCapacity(int requiredLength) {
        if (_payload is not null && _payload.Length >= requiredLength) {
            return;
        }

        int capacity = _payload is null
            ? Math.Max(4096, requiredLength)
            : Math.Max(requiredLength, checked(_payload.Length * 2));
        byte[] replacement = ArrayPool<byte>.Shared.Rent(capacity);
        if (_payload is not null) {
            _payload.AsSpan(0, _length).CopyTo(replacement);
            ArrayPool<byte>.Shared.Return(_payload);
        }

        _payload = replacement;
    }
}
