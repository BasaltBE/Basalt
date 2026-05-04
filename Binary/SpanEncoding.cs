using System.Buffers.Binary;

namespace Basalt.Binary
{
    public static partial class SpanEncodingExtensions {
        extension(Span<byte> source)
        {
            public void WriteInt8(sbyte value, int offset = 0)
            {
                source[offset] = unchecked((byte)value);
            }

            public void WriteUInt8(byte value, int offset = 0)
            {
                source[offset] = value;
            }

            public void WriteInt16(short value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteInt16LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteInt16BigEndian(source[offset..], value);
            }

            public void WriteUInt16(ushort value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteUInt16LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteUInt16BigEndian(source[offset..], value);
            }

            public void WriteInt32(int value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteInt32LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteInt32BigEndian(source[offset..], value);
            }

            public void WriteUInt32(uint value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteUInt32LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteUInt32BigEndian(source[offset..], value);
            }

            public void WriteInt64(long value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteInt64LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteInt64BigEndian(source[offset..], value);
            }

            public void WriteUInt64(ulong value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteUInt64LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteUInt64BigEndian(source[offset..], value);
            }
        }
        extension(ReadOnlySpan<byte> source)
        {
            public sbyte ReadInt8(int offset = 0)
            {
                return unchecked((sbyte)source[offset]);
            }

            public byte ReadUInt8(int offset = 0)
            {
                return source[offset];
            }

            public short ReadInt16(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadInt16LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadInt16BigEndian(source[offset..]);
            }

            public ushort ReadUInt16(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadUInt16LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadUInt16BigEndian(source[offset..]);
            }

            public int ReadInt32(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadInt32LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadInt32BigEndian(source[offset..]);
            }

            public uint ReadUInt32(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadUInt32LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadUInt32BigEndian(source[offset..]);
            }

            public long ReadInt64(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadInt64LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadInt64BigEndian(source[offset..]);
            }

            public ulong ReadUInt64(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) return BinaryPrimitives.ReadUInt64LittleEndian(source[offset..]);
                return BinaryPrimitives.ReadUInt64BigEndian(source[offset..]);
            }
        }
    }
}
