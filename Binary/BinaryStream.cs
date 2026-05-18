namespace Basalt.Binary
{
    public class BinaryStream(Memory<byte> buffer, int offset = default)
    {
        public Memory<byte> Buffer = buffer;
        public int Offset = offset;
        public int Length => Buffer.Length;
        public int Remaining => Length - Offset;
        public BinaryReader GetReader() => new(Buffer.Span, ref Offset);
        public BinaryWriter GetWriter() => new(Buffer.Span, ref Offset);
        public Memory<byte> GetRemainingBytes() => Buffer[Offset..];
        public Memory<byte> GetProcessedBytes() => Buffer[..Offset];
    }
}
