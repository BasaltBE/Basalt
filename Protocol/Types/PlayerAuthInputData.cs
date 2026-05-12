using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct PlayerAuthInputData : DataType
{
    public UInt128 Flags { get; set; }

    public PlayerAuthInputData(UInt128 flags)
    {
        Flags = flags;
    }

    public void SetFlag(PlayerAuthInputFlag flag, bool value)
    {
        UInt128 flagBit = UInt128.One << (int)flag;
        if (value)
        {
            Flags |= flagBit;
        }
        else
        {
            Flags &= ~flagBit;
        }
    }

    public bool HasFlag(PlayerAuthInputFlag flag)
    {
        UInt128 flagBit = UInt128.One << (int)flag;
        return (Flags & flagBit) != UInt128.Zero;
    }

    public List<PlayerAuthInputFlag> GetFlags()
    {
        List<PlayerAuthInputFlag> flags = [];
        for (int i = 0; i <= 64; i++)
        {
            PlayerAuthInputFlag flag = (PlayerAuthInputFlag)i;
            if (HasFlag(flag))
            {
                flags.Add(flag);
            }
        }

        return flags;
    }

    public void Read(ref BinaryReader reader)
    {
        Flags = UInt128.Zero;
        int shift = 0;

        while (true)
        {
            byte current = reader.ReadUInt8();
            UInt128 bits = (UInt128)(current & 0x7F);
            Flags |= bits << shift;

            if ((current & 0x80) == 0)
            {
                break;
            }

            shift += 7;
            if (shift > 126)
            {
                throw new FormatException("PlayerAuthInputData bitset overflows.");
            }
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        UInt128 value = Flags;
        while (value >= 0x80)
        {
            writer.WriteUInt8((byte)((ulong)(value & 0x7F) | 0x80));
            value >>= 7;
        }

        writer.WriteUInt8((byte)(ulong)value);
    }
}
