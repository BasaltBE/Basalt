using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.LongList)]
public sealed class LongListTag : BaseTag
{
    public List<long> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Write(BinaryWriter writer, TagOptions options)
    {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        WriteLength(writer, Values.Count, options.VarInt);
        foreach (long value in CollectionsMarshal.AsSpan(Values))
        {
            if (options.VarInt)
                writer.WriteZigZong(value);
            else
                writer.WriteInt64(value, true);
        }
    }

    public static LongListTag Read(BinaryReader reader, TagOptions options = default)
    {
        LongListTag tag = new()
        {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };

        int length = ReadLength(reader, options.VarInt);
        tag.Values.Capacity = length;
        for (int i = 0; i < length; i++)
            tag.Values.Add(options.VarInt ? reader.ReadZigZong() : reader.ReadInt64(true));

        return tag;
    }
}
