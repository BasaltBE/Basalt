using System.Text;
using Basalt.Binary;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt;

public static class Nbt {
    public static Tag Read(ReadOnlySpan<byte> buffer, ref int offset, Options? options = null) {
        BinaryReader reader = new(buffer, ref offset);
        return Read(ref reader, options);
    }

    public static Tag Read(BinaryStream stream, Options? options = null) {
        ArgumentNullException.ThrowIfNull(stream);
        BinaryReader reader = stream.GetReader();
        return Read(ref reader, options);
    }

    public static Tag Read(ref BinaryReader reader, Options? options = null) {
        options ??= Options.LittleEndian;
        TagType type = ReadTagType(ref reader);
        if (type == TagType.End) return new EndTag();
        string name = options.HasName ? ReadString(ref reader, options) : string.Empty;
        Tag tag = Read(ref reader, type, options);
        tag.Name = name;
        return tag;
    }

    internal static Tag Read(ref BinaryReader reader, TagType type, Options options) {
        Tag tag = type switch {
            TagType.Byte => new ByteTag(),
            TagType.Short => new ShortTag(),
            TagType.Int => new IntTag(),
            TagType.Long => new LongTag(),
            TagType.Float => new FloatTag(),
            TagType.Double => new DoubleTag(),
            TagType.ByteArray => new ByteArrayTag(),
            TagType.String => new StringTag(),
            TagType.List => new ListTag(),
            TagType.Compound => new CompoundTag(),
            TagType.IntArray => new IntArrayTag(),
            TagType.LongArray => new LongArrayTag(),
            _ => throw new InvalidDataException($"Unknown NBT tag type {(byte)type}.")
        };
        tag.Read(ref reader, options);
        return tag;
    }

    public static void Write(Tag tag, Span<byte> buffer, ref int offset, Options? options = null) {
        BinaryWriter writer = new(buffer, ref offset);
        Write(tag, ref writer, options);
    }

    public static void Write(BinaryStream stream, Tag tag, Options? options = null) {
        ArgumentNullException.ThrowIfNull(stream);
        BinaryWriter writer = stream.GetWriter();
        Write(tag, ref writer, options);
    }

    public static void Write(Tag tag, ref BinaryWriter writer, Options? options = null) {
        ArgumentNullException.ThrowIfNull(tag);
        options ??= Options.LittleEndian;
        writer.WriteUInt8((byte)tag.Type);
        if (tag.Type == TagType.End) return;
        if (options.HasName) WriteString(ref writer, tag.Name, options);
        Write(ref writer, tag, options);
    }

    internal static void Write(ref BinaryWriter writer, Tag tag, Options options) => tag.Write(ref writer, options);

    internal static int GetStringSize(string value, Options options) {
        int length = Encoding.UTF8.GetByteCount(value);
        if (length > 16 * 1024 * 1024)
            throw new InvalidDataException("NBT string length is invalid.");

        return checked((options.StringLengthVarInt ? GetVarUIntSize((uint)length) : sizeof(ushort)) + length);
    }

    internal static int GetLengthSize(int length, Options options) {
        if (length < 0 || length > 16 * 1024 * 1024)
            throw new InvalidDataException("NBT collection length is invalid.");

        return options.VarInt ? GetVarIntSize(length) : sizeof(int);
    }

    internal static int GetVarUIntSize(uint value) =>
        value < 1u << 7 ? 1 : value < 1u << 14 ? 2 : value < 1u << 21 ? 3 : value < 1u << 28 ? 4 : 5;

    internal static int GetVarIntSize(int value) =>
        GetVarUIntSize(unchecked((uint)((value << 1) ^ (value >> 31))));

    internal static int GetVarIntSize(long value) {
        ulong encoded = unchecked((ulong)((value << 1) ^ (value >> 63)));
        return encoded < 1UL << 7 ? 1 : encoded < 1UL << 14 ? 2 : encoded < 1UL << 21 ? 3 :
            encoded < 1UL << 28 ? 4 : encoded < 1UL << 35 ? 5 : encoded < 1UL << 42 ? 6 :
            encoded < 1UL << 49 ? 7 : encoded < 1UL << 56 ? 8 : encoded < 1UL << 63 ? 9 : 10;
    }

    internal static TagType ReadTagType(ref BinaryReader reader) {
        byte type = reader.ReadUInt8();
        if (type > (byte)TagType.LongArray) throw new InvalidDataException($"Unknown NBT tag type {type}.");
        return (TagType)type;
    }

    internal static int ReadLength(ref BinaryReader reader, Options options) {
        int length = options.VarInt ? reader.ReadZigZag() : reader.ReadInt32(options.ByteOrder == ByteOrder.LittleEndian);
        if (length < 0 || length > 16 * 1024 * 1024) throw new InvalidDataException("NBT collection length is invalid.");
        return length;
    }

    internal static void WriteLength(ref BinaryWriter writer, int length, Options options) {
        if (length < 0 || length > 16 * 1024 * 1024) throw new InvalidDataException("NBT collection length is invalid.");
        if (options.VarInt) writer.WriteZigZag(length);
        else writer.WriteInt32(length, options.ByteOrder == ByteOrder.LittleEndian);
    }

    internal static string ReadString(ref BinaryReader reader, Options options) {
        string value = options.StringLengthVarInt ? reader.ReadVarString() : reader.ReadString16(options.ByteOrder == ByteOrder.LittleEndian);
        if (value.Length > 16 * 1024 * 1024) throw new InvalidDataException("NBT string length is invalid.");
        return value;
    }

    internal static void WriteString(ref BinaryWriter writer, string value, Options options) {
        if (Encoding.UTF8.GetByteCount(value) > 16 * 1024 * 1024) throw new InvalidDataException("NBT string length is invalid.");
        if (options.StringLengthVarInt) writer.WriteVarString(value);
        else writer.WriteString16(value, options.ByteOrder == ByteOrder.LittleEndian);
    }

    internal static byte[] ReadBytes(ref BinaryReader reader, int length) => reader.ReadBytes(length).ToArray();
}
