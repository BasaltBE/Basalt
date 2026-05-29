using System;

namespace Basalt.Binary
{
    public ref struct BinaryReader(ReadOnlySpan<byte> buffer, ref int offset)
    {
        public readonly ReadOnlySpan<byte> Buffer = buffer;
        public ref int Offset = ref offset;
        public readonly int Length => Buffer.Length;
        public readonly int Remaining => Length - Offset;
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
        public readonly ReadOnlySpan<byte> GetProcessedBytes() => Buffer[..Offset];
        public readonly ReadOnlySpan<byte> GetRemainingBytes() => Buffer[Offset..];

        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            if ((uint)length > (uint)Remaining)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            ReadOnlySpan<byte> result = Buffer.Slice(Offset, length);
            Offset += length;
            return result;
        }

        public sbyte ReadInt8()
        {
            sbyte value = Buffer.ReadInt8(Offset);
            Offset += sizeof(sbyte);
            return value;
        }

        public byte ReadUInt8()
        {
            byte value = Buffer.ReadUInt8(Offset);
            Offset += sizeof(byte);
            return value;
        }

        public bool ReadBool()
        {
            bool value = Buffer.ReadBool(Offset);
            Offset += sizeof(byte);
            return value;
        }

        public short ReadInt16(bool littleEndian = false)
        {
            short value = Buffer.ReadInt16(Offset, littleEndian);
            Offset += sizeof(short);
            return value;
        }

        public ushort ReadUInt16(bool littleEndian = false)
        {
            ushort value = Buffer.ReadUInt16(Offset, littleEndian);
            Offset += sizeof(ushort);
            return value;
        }

        public int ReadInt32(bool littleEndian = false)
        {
            int value = Buffer.ReadInt32(Offset, littleEndian);
            Offset += sizeof(int);
            return value;
        }

        public uint ReadUInt32(bool littleEndian = false)
        {
            uint value = Buffer.ReadUInt32(Offset, littleEndian);
            Offset += sizeof(uint);
            return value;
        }

        public long ReadInt64(bool littleEndian = false)
        {
            long value = Buffer.ReadInt64(Offset, littleEndian);
            Offset += sizeof(long);
            return value;
        }

        public ulong ReadUInt64(bool littleEndian = false)
        {
            ulong value = Buffer.ReadUInt64(Offset, littleEndian);
            Offset += sizeof(ulong);
            return value;
        }

        public Half ReadF16(bool littleEndian = false)
        {
            Half value = Buffer.ReadF16(Offset, littleEndian);
            Offset += sizeof(short);
            return value;
        }

        public float ReadF32(bool littleEndian = false)
        {
            float value = Buffer.ReadF32(Offset, littleEndian);
            Offset += sizeof(int);
            return value;
        }

        public double ReadF64(bool littleEndian = false)
        {
            double value = Buffer.ReadF64(Offset, littleEndian);
            Offset += sizeof(long);
            return value;
        }

        public uint ReadVarUInt()
        {
            uint value = Buffer.ReadVarUInt(out int bytesRead, Offset);
            Offset += bytesRead;
            return value;
        }

        public int ReadVarInt()
        {
            int value = Buffer.ReadVarInt(out int bytesRead, Offset);
            Offset += bytesRead;
            return value;
        }

        public ulong ReadVarULong()
        {
            ulong value = 0;
            int shift = 0;
            for (int i = 0; i < 10; i++)
            {
                byte current = Buffer[Offset + i];
                value |= (ulong)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                {
                    Offset += i + 1;
                    return value;
                }

                shift += 7;
            }

            throw new FormatException("VarULong is too long.");
        }

        public long ReadVarLong() => SpanEncodingExtensions.ZigZong(ReadVarULong());
        public int ReadZigZag() => SpanEncodingExtensions.ZigZag(ReadVarUInt());
        public long ReadZigZong() => SpanEncodingExtensions.ZigZong(ReadVarULong());

        public string ReadString16(bool littleEndian = false)
        {
            ushort length = ReadUInt16(littleEndian);
            string value = Buffer.ReadString(length, Offset);
            Offset += length;
            return value;
        }

        public string ReadVarString()
        {
            int length = checked((int)ReadVarUInt());
            string value = Buffer.ReadString(length, Offset);
            Offset += length;
            return value;
        }

        public string ReadString32(bool littleEndian = false)
        {
            int length = checked((int)ReadUInt32(littleEndian));
            string value = Buffer.ReadString(length, Offset);
            Offset += length;
            return value;
        }
    }
}
