namespace Basalt.Binary
{
    public ref struct BinaryWriter(Span<byte> buffer, ref int offset)
    {
        public readonly Span<byte> Buffer = buffer;
        public ref int Offset = ref offset;
        public readonly int Length => Buffer.Length;
        public readonly int Remaining => Length - Offset;

        public static implicit operator BinaryReader(BinaryWriter writer) => new(writer.Buffer, ref writer.Offset);
        public void Reset() => Offset = 0;
        public void Seek(int offset)
        {
            if ((uint)offset > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            Offset = offset;
        }

        public void Advance(int count) => Seek(Offset + count);
        public readonly Span<byte> GetProcessedBytes() => Buffer[..Offset];
        public readonly Span<byte> GetRemainingBytes() => Buffer[Offset..];
        public readonly Range GetProcessedRange() => new(0, Offset);

        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            if (value.Length > Remaining)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            value.CopyTo(Buffer[Offset..]);
            Offset += value.Length;
        }

        public void WriteInt8(sbyte value)
        {
            Buffer.WriteInt8(value, Offset);
            Offset += sizeof(sbyte);
        }

        public void WriteUInt8(byte value)
        {
            Buffer.WriteUInt8(value, Offset);
            Offset += sizeof(byte);
        }

        public void WriteBool(bool value)
        {
            Buffer.WriteBool(value, Offset);
            Offset += sizeof(byte);
        }

        public void WriteInt16(short value, bool littleEndian = false)
        {
            Buffer.WriteInt16(value, Offset, littleEndian);
            Offset += sizeof(short);
        }

        public void WriteUInt16(ushort value, bool littleEndian = false)
        {
            Buffer.WriteUInt16(value, Offset, littleEndian);
            Offset += sizeof(ushort);
        }

        public void WriteInt32(int value, bool littleEndian = false)
        {
            Buffer.WriteInt32(value, Offset, littleEndian);
            Offset += sizeof(int);
        }

        public void WriteUInt32(uint value, bool littleEndian = false)
        {
            Buffer.WriteUInt32(value, Offset, littleEndian);
            Offset += sizeof(uint);
        }

        public void WriteInt64(long value, bool littleEndian = false)
        {
            Buffer.WriteInt64(value, Offset, littleEndian);
            Offset += sizeof(long);
        }

        public void WriteUInt64(ulong value, bool littleEndian = false)
        {
            Buffer.WriteUInt64(value, Offset, littleEndian);
            Offset += sizeof(ulong);
        }

        public void WriteF16(Half value, bool littleEndian = false)
        {
            Buffer.WriteF16(value, Offset, littleEndian);
            Offset += sizeof(short);
        }

        public void WriteF32(float value, bool littleEndian = false)
        {
            Buffer.WriteF32(value, Offset, littleEndian);
            Offset += sizeof(int);
        }

        public void WriteF64(double value, bool littleEndian = false)
        {
            Buffer.WriteF64(value, Offset, littleEndian);
            Offset += sizeof(long);
        }

        public int WriteVarUInt(uint value)
        {
            int bytesWritten = Buffer.WriteVarUInt(value, Offset);
            Offset += bytesWritten;
            return bytesWritten;
        }

        public int WriteVarInt(int value)
        {
            int bytesWritten = Buffer.WriteVarInt(value, Offset);
            Offset += bytesWritten;
            return bytesWritten;
        }

        public int WriteVarULong(ulong value)
        {
            int bytesWritten = 0;
            while (value >= 0x80)
            {
                Buffer[Offset + bytesWritten] = (byte)((byte)value | 0x80);
                value >>= 7;
                bytesWritten++;
            }

            Buffer[Offset + bytesWritten] = (byte)value;
            bytesWritten++;
            Offset += bytesWritten;
            return bytesWritten;
        }

        public int WriteVarLong(long value) => WriteVarULong(SpanEncodingExtensions.ZigZong(value));
        public int WriteZigZag(int value) => WriteVarUInt(SpanEncodingExtensions.ZigZag(value));
        public int WriteZigZong(long value) => WriteVarULong(SpanEncodingExtensions.ZigZong(value));

        public int WriteString16(ReadOnlySpan<char> value, bool littleEndian = false)
        {
            int length = System.Text.Encoding.UTF8.GetByteCount(value);
            if (length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            WriteUInt16((ushort)length, littleEndian);
            return sizeof(ushort) + WriteStringRaw(value, length);
        }

        public int WriteVarString(ReadOnlySpan<char> value)
        {
            int length = System.Text.Encoding.UTF8.GetByteCount(value);
            int prefix = WriteVarUInt((uint)length);
            return prefix + WriteStringRaw(value, length);
        }

        public int WriteString32(ReadOnlySpan<char> value, bool littleEndian = false)
        {
            int length = System.Text.Encoding.UTF8.GetByteCount(value);
            WriteUInt32((uint)length, littleEndian);
            return sizeof(uint) + WriteStringRaw(value, length);
        }

        private int WriteStringRaw(ReadOnlySpan<char> value, int length)
        {
            int written = Buffer.WriteString(value, length, Offset);
            Offset += written;
            return written;
        }
    }
}
