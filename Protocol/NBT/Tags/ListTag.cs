using Basalt.Protocol.Io;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.List)]
public sealed class ListTag : BaseTag {
    public List<BaseTag> Values { get; } = [];

    public override object ToJsonValue() => Values.Select(v => v.ToJsonValue()).ToList();

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        TagType elementType = Values.Count == 0 ? TagType.Byte : Values[0].Type;
        TagOptions payloadOptions = options with { Name = false, Type = false };

        writer.WriteInt8((sbyte)elementType);
        WriteLength(writer, Values.Count, options.VarInt);

        foreach (BaseTag value in Values) {
            if (value.Type != elementType)
                throw new InvalidOperationException("NBT list elements must share a single type.");

            NBT.WriteTag(writer, value, payloadOptions);
        }
    }

    public static ListTag Read(BinaryReader reader, TagOptions options = default) {
        ListTag tag = new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };

        TagType elementType = (TagType)reader.ReadInt8();
        int length = ReadLength(reader, options.VarInt);
        TagOptions payloadOptions = options with { Name = false, Type = false };

        tag.Values.Capacity = length;
        for (int i = 0; i < length; i++)
            tag.Values.Add(NBT.ReadTag(reader, elementType, payloadOptions));

        return tag;
    }
}
