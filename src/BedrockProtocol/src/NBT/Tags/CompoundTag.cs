using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.NBT;
[Tag(TagType.Compound)]
public class CompoundTag : BaseTag {
    public Dictionary<string, BaseTag> Values { get; } = new(StringComparer.Ordinal);

    public CompoundTag Clone() {
        CompoundTag clone = new() { Name = Name };
        foreach ((string key, BaseTag value) in Values) {
            clone.Values[key] = CloneValue(value);
        }

        return clone;
    }

    public T? Get<T>(string key) where T : BaseTag {
        return Values.TryGetValue(key, out BaseTag? value) ? value as T : null;
    }

    public void Set(string key, BaseTag value) {
        value.Name = key;
        Values[key] = value;
    }

    private static BaseTag CloneValue(BaseTag value) {
        return value switch {
            CompoundTag compound => compound.Clone(),
            ListTag list => CloneList(list),
            ByteTag byteTag => new ByteTag { Name = byteTag.Name, Value = byteTag.Value },
            ShortTag shortTag => new ShortTag { Name = shortTag.Name, Value = shortTag.Value },
            IntTag intTag => new IntTag { Name = intTag.Name, Value = intTag.Value },
            LongTag longTag => new LongTag { Name = longTag.Name, Value = longTag.Value },
            FloatTag floatTag => new FloatTag { Name = floatTag.Name, Value = floatTag.Value },
            DoubleTag doubleTag => new DoubleTag { Name = doubleTag.Name, Value = doubleTag.Value },
            StringTag stringTag => new StringTag { Name = stringTag.Name, Value = stringTag.Value },
            ByteListTag byteList => CloneByteList(byteList),
            IntListTag intList => CloneIntList(intList),
            LongListTag longList => CloneLongList(longList),
            EndTag end => new EndTag { Name = end.Name },
            _ => throw new InvalidOperationException($"Unsupported NBT tag type: {value.Type}.")
        };
    }

    private static ListTag CloneList(ListTag list) {
        ListTag clone = new() { Name = list.Name };
        for (int i = 0; i < list.Values.Count; i++) {
            clone.Values.Add(CloneValue(list.Values[i]));
        }

        return clone;
    }

    private static ByteListTag CloneByteList(ByteListTag list) {
        ByteListTag clone = new() { Name = list.Name };
        clone.Values.AddRange(list.Values);
        return clone;
    }

    private static IntListTag CloneIntList(IntListTag list) {
        IntListTag clone = new() { Name = list.Name };
        clone.Values.AddRange(list.Values);
        return clone;
    }

    private static LongListTag CloneLongList(LongListTag list) {
        LongListTag clone = new() { Name = list.Name };
        clone.Values.AddRange(list.Values);
        return clone;
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
