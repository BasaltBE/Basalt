using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.IO;

public static class NBT
{
    public static T Read<T>(BinaryReader reader, ReadWriteOptions options, bool canHaveName = true) where T : BaseTag
    {
        TagType rootType = (TagType)reader.ReadInt8();
        BaseTag tag = ReadTag(reader, rootType, options, canHaveName);

        if (tag is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Unexpected root NBT tag type '{rootType}' for requested '{typeof(T).Name}'.");
    }

    public static CompoundTag ReadRootCompoundTag(BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        TagType rootType = (TagType)reader.ReadInt8();
        if (rootType != TagType.Compound)
        {
            throw new InvalidOperationException($"Unexpected root NBT tag type '{rootType}'.");
        }

        return CompoundTag.Read(reader, options, canHaveName);
    }

    public static BaseTag ReadTag(BinaryReader reader, TagType type, ReadWriteOptions options, bool canHaveName)
    {
        return type switch
        {
            TagType.End => EndTag.Read(reader, options, canHaveName),
            TagType.Byte => ByteTag.Read(reader, options, canHaveName),
            TagType.Short => ShortTag.Read(reader, options, canHaveName),
            TagType.Int => IntTag.Read(reader, options, canHaveName),
            TagType.Long => LongTag.Read(reader, options, canHaveName),
            TagType.Float => FloatTag.Read(reader, options, canHaveName),
            TagType.Double => DoubleTag.Read(reader, options, canHaveName),
            TagType.ByteList => ByteListTag.Read(reader, options, canHaveName),
            TagType.String => StringTag.Read(reader, options, canHaveName),
            TagType.List => ListTag.Read(reader, options, canHaveName),
            TagType.Compound => CompoundTag.Read(reader, options, canHaveName),
            TagType.IntList => IntListTag.Read(reader, options, canHaveName),
            TagType.LongList => LongListTag.Read(reader, options, canHaveName),
            _ => throw new InvalidOperationException($"Unsupported NBT tag type: {type}.")
        };
    }

    public static void WriteTag(BinaryWriter writer, BaseTag tag, ReadWriteOptions options, bool canHaveName)
    {
        if (options.Type)
        {
            writer.WriteInt8((sbyte)tag.Type);
        }

        tag.Write(writer, options, canHaveName);
    }
}

