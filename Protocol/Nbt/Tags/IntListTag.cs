using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class IntListTag : BaseTag
{
    public override TagType Type => TagType.IntList;
    public List<int> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }

        WriteLength(writer, Values.Count, options.VarInt);
        ReadOnlySpan<int> span = CollectionsMarshal.AsSpan(Values);
        for (int i = 0; i < span.Length; i++)
        {
            if (options.VarInt)
            {
                writer.WriteZigZag(span[i]);
            }
            else
            {
                writer.WriteInt32(span[i], true);
            }
        }
    }

    public static IntListTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        IntListTag tag = new IntListTag
        {
            Name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null
        };

        int length = ReadLength(reader, effective.VarInt);
        tag.Values.Capacity = Math.Max(tag.Values.Capacity, length);
        for (int i = 0; i < length; i++)
        {
            tag.Values.Add(effective.VarInt ? reader.ReadZigZag() : reader.ReadInt32(true));
        }

        return tag;
    }
}


