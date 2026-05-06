using Basalt.Protocol.IO;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class ListTag : BaseTag
{
    public override TagType Type => TagType.List;
    public List<BaseTag> Values { get; } = [];

    public override object ToJsonValue()
    {
        List<object?> values = new(Values.Count);
        for (int i = 0; i < Values.Count; i++)
        {
            values.Add(Values[i].ToJsonValue());
        }

        return values;
    }

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        TagType elementType = (TagType)reader.ReadInt8();
        int length = ReadLength(ref reader, options.VarInt);

        Values.Clear();
        Values.Capacity = Math.Max(Values.Capacity, length);
        for (int i = 0; i < length; i++)
        {
            BaseTag item = NBT.ReadTag(ref reader, elementType, options with { Name = false, Type = false }, false);
            Values.Add(item);
        }
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        TagType elementType = Values.Count == 0 ? TagType.Byte : Values[0].Type;
        writer.WriteInt8((sbyte)elementType);
        WriteLength(ref writer, Values.Count, options.VarInt);

        for (int i = 0; i < Values.Count; i++)
        {
            if (Values[i].Type != elementType)
            {
                throw new InvalidOperationException("NBT list elements must share a single type.");
            }

            NBT.WriteTag(ref writer, Values[i], options with { Name = false, Type = false }, false);
        }
    }
}
