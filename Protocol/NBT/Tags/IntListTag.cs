using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.IntList)]
public sealed class IntListTag : BaseTag
{
    public List<int> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Write(BinaryWriter writer, TagOptions options)
    {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        WriteLength(writer, Values.Count, options.VarInt);
        foreach (int value in CollectionsMarshal.AsSpan(Values))
        {
            if (options.VarInt)
                writer.WriteZigZag(value);
            else
                writer.WriteInt32(value, true);
        }
    }

    public static IntListTag Read(BinaryReader reader, TagOptions options = default)
    {
        IntListTag tag = new()
        {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };

        int length = ReadLength(reader, options.VarInt);
        tag.Values.Capacity = length;
        for (int i = 0; i < length; i++)
            tag.Values.Add(options.VarInt ? reader.ReadZigZag() : reader.ReadInt32(true));

        return tag;
    }
}
