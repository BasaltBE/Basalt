using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class IntListTag : BaseTag
{
    public override TagType Type => TagType.IntList;
    public List<int> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        int length = ReadLength(ref reader, options.VarInt);
        Values.Clear();
        for (int i = 0; i < length; i++)
        {
            Values.Add(reader.ReadInt32(true));
        }
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        WriteLength(ref writer, Values.Count, options.VarInt);
        ReadOnlySpan<int> span = CollectionsMarshal.AsSpan(Values);
        for (int i = 0; i < span.Length; i++)
        {
            writer.WriteInt32(span[i], true);
        }
    }
}
