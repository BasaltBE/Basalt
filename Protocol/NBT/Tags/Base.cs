using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public abstract class BaseTag {
    private static readonly ConcurrentDictionary<Type, TagType> TypeCache = [];

    public TagType Type {
        get {
            Type type = GetType();
            if (TypeCache.TryGetValue(type, out TagType tagType))
                return tagType;

            TagAttribute? attribute = type.GetCustomAttribute<TagAttribute>();
            if (attribute is null)
                throw new InvalidOperationException($"{type.FullName} is missing TagAttribute.");

            return TypeCache.GetOrAdd(type, attribute.Type);
        }
    }

    public string? Name;

    public abstract object? ToJsonValue();
    public abstract void Write(BinaryWriter writer, TagOptions options);

    protected static string ReadString(BinaryReader reader, bool varInt) {
        int length;
        if (varInt) {
            uint raw = reader.ReadVarUInt();
            if (raw > short.MaxValue || raw > reader.Remaining)
                throw new FormatException("Invalid NBT string length.");

            length = (int)raw;
        }
        else {
            short raw = reader.ReadInt16(true);
            if (raw < 0)
                throw new FormatException("Negative NBT string length.");

            length = raw;
        }

        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }

    protected static void WriteString(BinaryWriter writer, string value, bool varInt) {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount);
        try {
            Encoding.UTF8.GetBytes(value, rentedBytes);

            if (varInt) {
                writer.WriteVarUInt((uint)byteCount);
            }
            else {
                if (byteCount > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "NBT string is too long for Int16 length.");

                writer.WriteInt16((short)byteCount, true);
            }

            writer.WriteBytes(rentedBytes.AsSpan(0, byteCount));
        }
        finally {
            ArrayPool<byte>.Shared.Return(rentedBytes);
        }
    }

    protected static int ReadLength(BinaryReader reader, bool varInt) {
        int length = varInt ? reader.ReadZigZag() : reader.ReadInt32(true);
        if (length < 0)
            throw new FormatException("Negative NBT length.");

        return length;
    }

    protected static void WriteLength(BinaryWriter writer, int length, bool varInt) {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (varInt)
            writer.WriteZigZag(length);
        else
            writer.WriteInt32(length, true);
    }
}
