using Basalt.Protocol.IO;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class CompoundTag : BaseTag
{
    public override TagType Type => TagType.Compound;
    public Dictionary<string, BaseTag> Values { get; } = new(StringComparer.Ordinal);

    public T? Get<T>(string key) where T : BaseTag
    {
        return Values.TryGetValue(key, out BaseTag? value) ? value as T : null;
    }

    public void Set(string key, BaseTag value)
    {
        value.Name = key;
        Values[key] = value;
    }

    public override object ToJsonValue()
    {
        Dictionary<string, object?> json = new(StringComparer.Ordinal);
        foreach ((string key, BaseTag value) in Values)
        {
            json[key] = value.ToJsonValue();
        }

        return json;
    }

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        Values.Clear();

        while (true)
        {
            TagType type = (TagType)reader.ReadInt8();
            if (type == TagType.End)
            {
                break;
            }

            BaseTag child = NBT.ReadTag(ref reader, type, options with { Name = true, Type = false }, true);
            Values[child.Name ?? string.Empty] = child;
        }
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        foreach ((string key, BaseTag value) in Values)
        {
            value.Name = key;
            NBT.WriteTag(ref writer, value, options with { Name = true, Type = true }, true);
        }

        writer.WriteInt8((sbyte)TagType.End);
    }
}
