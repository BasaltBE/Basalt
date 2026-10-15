using System.Buffers;
using System.Security.Cryptography;

namespace BasaltBE.Basalt.Network.Types;

internal sealed class PooledBufferStream : Stream {
    byte[] buffer;
    int length;

    internal PooledBufferStream(int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        buffer = ArrayPool<byte>.Shared.Rent(capacity);
    }

    internal ReadOnlyMemory<byte> WrittenMemory => buffer.AsMemory(0, length);
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => buffer.Length != 0;
    public override long Length {
        get {
            ObjectDisposedException.ThrowIf(buffer.Length == 0, this);
            return length;
        }
    }
    public override long Position {
        get => Length;
        set => throw new NotSupportedException();
    }

    public override void Write(ReadOnlySpan<byte> value) {
        ObjectDisposedException.ThrowIf(buffer.Length == 0, this);
        int required = checked(length + value.Length);
        if (required > buffer.Length) {
            byte[] expanded = ArrayPool<byte>.Shared.Rent(Math.Max(required, checked(buffer.Length * 2)));
            buffer.AsSpan(0, length).CopyTo(expanded);
            value.CopyTo(expanded.AsSpan(length));
            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = expanded;
            length = required;
            return;
        }
        value.CopyTo(buffer.AsSpan(length));
        length = required;
    }

    public override void Write(byte[] value, int offset, int count) {
        ArgumentNullException.ThrowIfNull(value);
        Write(value.AsSpan(offset, count));
    }

    public override void WriteByte(byte value) {
        Span<byte> single = stackalloc byte[1];
        single[0] = value;
        Write(single);
    }

    public override void Flush() => ObjectDisposedException.ThrowIf(buffer.Length == 0, this);
    public override int Read(byte[] value, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing) {
        if (buffer.Length != 0) {
            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
            length = 0;
        }
        base.Dispose(disposing);
    }
}
