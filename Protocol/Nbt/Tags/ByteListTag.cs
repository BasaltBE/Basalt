using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class ByteListTag : BaseTag
{
    public override TagType Type => TagType.ByteList;
    public List<byte> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }

        WriteLength(writer, Values.Count, options.VarInt);
        writer.WriteBytes(CollectionsMarshal.AsSpan(Values));
    }

    public static ByteListTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        ByteListTag tag = new ByteListTag
        {
            Name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null
        };

        int length = ReadLength(reader, effective.VarInt);
        ReadOnlySpan<byte> bytes = reader.ReadBytes(length);
        tag.Values.Capacity = Math.Max(tag.Values.Capacity, length);
        for (int i = 0; i < bytes.Length; i++)
        {
            tag.Values.Add(bytes[i]);
        }

        return tag;
    }
}


