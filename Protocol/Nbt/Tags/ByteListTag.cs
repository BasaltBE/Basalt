using System.Runtime.InteropServices;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class ByteListTag : BaseTag
{
    public override TagType Type => TagType.ByteList;
    public List<byte> Values { get; } = [];
    public override object ToJsonValue() => Values;

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        int length = ReadLength(ref reader, options.VarInt);
        ReadOnlySpan<byte> bytes = reader.ReadBytes(length);
        Values.Clear();
        for (int i = 0; i < bytes.Length; i++)
        {
            Values.Add(bytes[i]);
        }
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        WriteLength(ref writer, Values.Count, options.VarInt);
        writer.WriteBytes(CollectionsMarshal.AsSpan(Values));
    }
}
