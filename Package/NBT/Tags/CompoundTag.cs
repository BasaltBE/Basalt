using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Nbt;

[Tag(TagType.Compound)]
public class CompoundTag : BaseTag {
    public Dictionary<string, BaseTag> Values { get; } = new(StringComparer.Ordinal);

    public T? Get<T>(string key) where T : BaseTag {
        return Values.TryGetValue(key, out BaseTag? value) ? value as T : null;
    }

    public void Set(string key, BaseTag value) {
        value.Name = key;
        Values[key] = value;
    }

    public override object ToJsonValue() {
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        foreach ((string key, BaseTag value) in Values)
            result[key] = value.ToJsonValue();

        return result;
    }

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        TagOptions payloadOptions = options with { Name = false, Type = false };
        foreach ((string key, BaseTag value) in Values) {
            writer.WriteInt8((sbyte)value.Type);
            WriteString(writer, key, options.VarInt);
            NBT.WriteTag(writer, value, payloadOptions);
        }

        writer.WriteInt8((sbyte)TagType.End);
    }

    public static CompoundTag Read(BinaryReader reader, TagOptions options = default) {
        CompoundTag tag = new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };

        TagOptions payloadOptions = options with { Name = false, Type = false };
        while (true) {
            TagType type = (TagType)reader.ReadInt8();
            if (type == TagType.End)
                break;

            string key = ReadString(reader, options.VarInt);
            BaseTag child = NBT.ReadTag(reader, type, payloadOptions);
            child.Name = key;
            tag.Values[key] = child;
        }

        return tag;
    }
}
