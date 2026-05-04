using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Basalt.Binary
{
    public static partial class SpanEncodingExtensions {
#if BIGENDIAN
        public const bool IsLittleEndian = false;
#else
        public const bool IsLittleEndian = true;
#endif
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
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
            }

            public void WriteUInt16(ushort value, int offset = 0, bool littleEndian = true)
            {
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
            }

            public void WriteInt32(int value, int offset = 0, bool littleEndian = true)
            {
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
            }

            public void WriteUInt32(uint value, int offset = 0, bool littleEndian = true)
            {
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
            }

            public void WriteInt64(long value, int offset = 0, bool littleEndian = true)
            {
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
            }

            public void WriteUInt64(ulong value, int offset = 0, bool littleEndian = true)
            {
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                MemoryMarshal.Write(source[offset..], value);
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
                short value = MemoryMarshal.Read<short>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }

            public ushort ReadUInt16(int offset = 0, bool littleEndian = true)
            {
                ushort value = MemoryMarshal.Read<ushort>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }

            public int ReadInt32(int offset = 0, bool littleEndian = true)
            {
                int value = MemoryMarshal.Read<int>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }

            public uint ReadUInt32(int offset = 0, bool littleEndian = true)
            {
                uint value = MemoryMarshal.Read<uint>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }

            public long ReadInt64(int offset = 0, bool littleEndian = true)
            {
                long value = MemoryMarshal.Read<long>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }

            public ulong ReadUInt64(int offset = 0, bool littleEndian = true)
            {
                ulong value = MemoryMarshal.Read<ulong>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);
                return value;
            }
        }
    }
}
