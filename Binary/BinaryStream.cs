using System.Buffers;

namespace Basalt.Binary
{
    public class BinaryStream: IDisposable
    {
        public static BinaryStream Rent(int minBytes) => new(MemoryPool<byte>.Shared.Rent(minBytes));
        public readonly IMemoryOwner<byte>? Owner;
        public readonly Memory<byte> Buffer;

        public int Offset;
        public int Length => Buffer.Length;
        public int Remaining => Length - Offset;

        public BinaryStream(IMemoryOwner<byte> owner, int offset = default)
        {
            Owner = owner;
            Buffer = owner.Memory;
            Offset = offset;
        }
        public BinaryStream(Memory<byte> buffer, int offset = default)
        {
            Owner = null;
            Buffer = buffer;
            Offset = offset;
        }

        public BinaryReader GetReader() => new(Buffer.Span, ref Offset);
        public BinaryWriter GetWriter() => new(Buffer.Span, ref Offset);
        public Memory<byte> GetProcessedBytes() => Buffer[..Offset];
        public Memory<byte> GetRemainingBytes() => Buffer[Offset..];

        void IDisposable.Dispose()
        {
            Owner?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static implicit operator BinaryReader(BinaryStream stream) => stream.GetReader();
        public static implicit operator BinaryWriter(BinaryStream stream) => stream.GetWriter();
    }

}
