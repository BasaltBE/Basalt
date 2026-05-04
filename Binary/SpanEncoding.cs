using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Basalt.Binary
{
    public static partial class SpanEncodingExtensions
    {
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

            public void WriteBool(bool value, int offset = 0)
            {
                source[offset] = value ? (byte)1 : (byte)0;
            }

            public void WriteUInt24(uint value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian)
                {
                    source[offset] = (byte)value;
                    source[offset + 1] = (byte)(value >> 8);
                    source[offset + 2] = (byte)(value >> 16);
                }
                else
                {
                    source[offset] = (byte)(value >> 16);
                    source[offset + 1] = (byte)(value >> 8);
                    source[offset + 2] = (byte)value;
                }
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

            public void WriteF16(Half value, int offset = 0, bool littleEndian = true)
            {
                short bits = BitConverter.HalfToInt16Bits(value);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                MemoryMarshal.Write(source[offset..], bits);
            }

            public void WriteF32(float value, int offset = 0, bool littleEndian = true)
            {
                int bits = BitConverter.SingleToInt32Bits(value);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                MemoryMarshal.Write(source[offset..], bits);
            }

            public void WriteF64(double value, int offset = 0, bool littleEndian = true)
            {
                long bits = BitConverter.DoubleToInt64Bits(value);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                MemoryMarshal.Write(source[offset..], bits);
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

            public bool ReadBool(int offset = 0)
            {
                return source[offset] != 0;
            }

            public uint ReadUInt24(int offset = 0, bool littleEndian = true)
            {
                if (littleEndian)
                    return (uint)(source[offset] | (source[offset + 1] << 8) | (source[offset + 2] << 16));
                return (uint)((source[offset] << 16) | (source[offset + 1] << 8) | source[offset + 2]);
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

            public Half ReadF16(int offset = 0, bool littleEndian = true)
            {
                short bits = MemoryMarshal.Read<short>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                return BitConverter.Int16BitsToHalf(bits);
            }

            public float ReadF32(int offset = 0, bool littleEndian = true)
            {
                int bits = MemoryMarshal.Read<int>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                return BitConverter.Int32BitsToSingle(bits);
            }

            public double ReadF64(int offset = 0, bool littleEndian = true)
            {
                long bits = MemoryMarshal.Read<long>(source[offset..]);
                if (IsLittleEndian != littleEndian)
                    bits = BinaryPrimitives.ReverseEndianness(bits);
                return BitConverter.Int64BitsToDouble(bits);
            }
        }
    }
}
