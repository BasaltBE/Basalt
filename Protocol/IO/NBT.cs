using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.IO;

public static class NBT
{
    public static BaseTag ReadTag(ref BinaryReader reader, TagType type, ReadWriteOptions options, bool canHaveName)
    {
        BaseTag tag = type switch
        {
            TagType.End => new EndTag(),
            TagType.Byte => new ByteTag(),
            TagType.Short => new ShortTag(),
            TagType.Int => new IntTag(),
            TagType.Long => new LongTag(),
            TagType.Float => new FloatTag(),
            TagType.Double => new DoubleTag(),
            TagType.ByteList => new ByteListTag(),
            TagType.String => new StringTag(),
            TagType.List => new ListTag(),
            TagType.Compound => new CompoundTag(),
            TagType.IntList => new IntListTag(),
            TagType.LongList => new LongListTag(),
            _ => throw new InvalidOperationException($"Unsupported NBT tag type: {type}.")
        };

        tag.Read(ref reader, options with { Type = false }, canHaveName);
        return tag;
    }

    public static void WriteTag(ref BinaryWriter writer, BaseTag tag, ReadWriteOptions options, bool canHaveName)
    {
        if (options.Type)
        {
            writer.WriteInt8((sbyte)tag.Type);
        }

        tag.Write(ref writer, options with { Type = false }, canHaveName);
    }
}
