using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.IO;

public static class NBT
{
    public static T Read<T>(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true) where T : BaseTag
    {
        TagType rootType = (TagType)reader.ReadInt8();
        BaseTag tag = ReadTag(ref reader, rootType, options, canHaveName);

        if (tag is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Unexpected root NBT tag type '{rootType}' for requested '{typeof(T).Name}'.");
    }

    public static CompoundTag ReadRootCompoundTag(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        TagType rootType = (TagType)reader.ReadInt8();
        if (rootType != TagType.Compound)
        {
            throw new InvalidOperationException($"Unexpected root NBT tag type '{rootType}'.");
        }

        return CompoundTag.Read(ref reader, options, canHaveName);
    }

    public static BaseTag ReadTag(ref BinaryReader reader, TagType type, ReadWriteOptions options, bool canHaveName)
    {
        return type switch
        {
            TagType.End => EndTag.Read(ref reader, options, canHaveName),
            TagType.Byte => ByteTag.Read(ref reader, options, canHaveName),
            TagType.Short => ShortTag.Read(ref reader, options, canHaveName),
            TagType.Int => IntTag.Read(ref reader, options, canHaveName),
            TagType.Long => LongTag.Read(ref reader, options, canHaveName),
            TagType.Float => FloatTag.Read(ref reader, options, canHaveName),
            TagType.Double => DoubleTag.Read(ref reader, options, canHaveName),
            TagType.ByteList => ByteListTag.Read(ref reader, options, canHaveName),
            TagType.String => StringTag.Read(ref reader, options, canHaveName),
            TagType.List => ListTag.Read(ref reader, options, canHaveName),
            TagType.Compound => CompoundTag.Read(ref reader, options, canHaveName),
            TagType.IntList => IntListTag.Read(ref reader, options, canHaveName),
            TagType.LongList => LongListTag.Read(ref reader, options, canHaveName),
            _ => throw new InvalidOperationException($"Unsupported NBT tag type: {type}.")
        };
    }

    public static void WriteTag(ref BinaryWriter writer, BaseTag tag, ReadWriteOptions options, bool canHaveName)
    {
        if (options.Type)
        {
            writer.WriteInt8((sbyte)tag.Type);
        }

        tag.Write(ref writer, options, canHaveName);
    }
}

