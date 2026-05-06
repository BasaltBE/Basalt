using Basalt.Protocol.IO;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public class CompoundTag : BaseTag
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

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        ReadWriteOptions payloadOptions = options with { Name = false, Type = false };
        foreach (KeyValuePair<string, BaseTag> entry in Values)
        {
            writer.WriteInt8((sbyte)entry.Value.Type);
            WriteName(ref writer, entry.Key, options.VarInt);
            NBT.WriteTag(ref writer, entry.Value, payloadOptions, false);
        }

        writer.WriteInt8((sbyte)TagType.End);
    }

    public static CompoundTag Read(ref BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        CompoundTag tag = new CompoundTag
        {
            Name = canHaveName && effective.Name ? ReadName(ref reader, effective.VarInt) : null
        };

        ReadWriteOptions payloadOptions = effective with { Name = false, Type = false };
        while (true)
        {
            TagType type = (TagType)reader.ReadInt8();
            if (type == TagType.End)
            {
                break;
            }

            string key = ReadName(ref reader, effective.VarInt);
            BaseTag child = NBT.ReadTag(ref reader, type, payloadOptions, false);
            child.Name = key;
            tag.Values[key] = child;
        }

        return tag;
    }
}


