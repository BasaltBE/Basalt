using System.Text;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public abstract class BaseTag
{
    public abstract TagType Type { get; }
    public string? Name { get; set; }

    public abstract object? ToJsonValue();
    public abstract void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true);

    protected static string ReadString(ref BinaryReader reader, bool varInt)
    {
        if (varInt)
        {
            int length = reader.ReadVarInt();
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        short length16 = reader.ReadInt16(true);
        if (length16 < 0)
        {
            throw new FormatException("Negative NBT string length.");
        }

        return Encoding.UTF8.GetString(reader.ReadBytes(length16));
    }

    protected static void WriteString(ref BinaryWriter writer, string value, bool varInt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (varInt)
        {
            writer.WriteVarInt(bytes.Length);
        }
        else
        {
            if (bytes.Length > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "NBT string is too long for Int16 length.");
            }

            writer.WriteInt16((short)bytes.Length, true);
        }

        writer.WriteBytes(bytes);
    }

    protected static string ReadName(ref BinaryReader reader, bool varInt) => ReadString(ref reader, varInt);

    protected static void WriteName(ref BinaryWriter writer, string? name, bool varInt) => WriteString(ref writer, name ?? string.Empty, varInt);

    protected static int ReadLength(ref BinaryReader reader, bool varInt)
    {
        int length = varInt ? reader.ReadZigZag() : reader.ReadInt32(true);
        if (length < 0)
        {
            throw new FormatException("Negative NBT length.");
        }

        return length;
    }

    protected static void WriteLength(ref BinaryWriter writer, int length, bool varInt)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (varInt)
        {
            writer.WriteZigZag(length);
        }
        else
        {
            writer.WriteInt32(length, true);
        }
    }
}

