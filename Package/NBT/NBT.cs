using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Nbt;

public static class NBT {
    public static T ReadTag<T>(BinaryReader reader, TagOptions options = default) where T : BaseTag {
        BaseTag tag = ReadTag(reader, options);
        if (tag is T typed)
            return typed;

        throw new InvalidOperationException($"Unexpected root NBT tag type '{tag.Type}' for requested '{typeof(T).Name}'.");
    }

    public static BaseTag ReadTag(BinaryReader reader, TagOptions options = default) {
        TagType type = (TagType)reader.ReadInt8();
        return ReadTag(reader, type, options with { Type = false });
    }

    public static BaseTag ReadTag(BinaryReader reader, TagType type, TagOptions options = default) {
        return type switch {
            TagType.End => EndTag.Read(reader, options),
            TagType.Byte => ByteTag.Read(reader, options),
            TagType.String => StringTag.Read(reader, options),
            TagType.Short => ShortTag.Read(reader, options),
            TagType.Int => IntTag.Read(reader, options),
            TagType.Long => LongTag.Read(reader, options),
            TagType.Float => FloatTag.Read(reader, options),
            TagType.Double => DoubleTag.Read(reader, options),
            TagType.ByteList => ByteListTag.Read(reader, options),
            TagType.List => ListTag.Read(reader, options),
            TagType.Compound => CompoundTag.Read(reader, options),
            TagType.IntList => IntListTag.Read(reader, options),
            TagType.LongList => LongListTag.Read(reader, options),
            _ => throw new InvalidOperationException($"Unsupported NBT tag type: {type}.")
        };
    }

    public static void WriteTag(BinaryWriter writer, BaseTag tag, TagOptions options = default) {
        if (options.Type)
            writer.WriteInt8((sbyte)tag.Type);

        tag.Write(writer, options with { Type = false });
    }
}
