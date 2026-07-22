using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.ByteList)]
public sealed class ByteListTag : BaseTag {
    public List<byte> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        WriteLength(writer, Values.Count, options.VarInt);
        writer.WriteBytes(CollectionsMarshal.AsSpan(Values));
    }

    public static ByteListTag Read(BinaryReader reader, TagOptions options = default) {
        ByteListTag tag = new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };

        int length = ReadLength(reader, options.VarInt);
        ReadOnlySpan<byte> bytes = reader.ReadBytes(length);
        tag.Values.Capacity = length;

        for (int i = 0; i < bytes.Length; i++)
            tag.Values.Add(bytes[i]);

        return tag;
    }
}
