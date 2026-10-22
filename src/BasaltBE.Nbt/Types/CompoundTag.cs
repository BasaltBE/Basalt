using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class CompoundTag : Tag {
    private readonly Dictionary<string, Tag> tags = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, Tag> Tags => tags;
    public override TagType Type => TagType.Compound;

    public CompoundTag Add(string name, Tag tag) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(tag);
        if (tag.Type == TagType.End)
            throw new ArgumentException("End tags cannot be stored in a compound.", nameof(tag));

        tag.Name = name;
        tags[name] = tag;
        return this;
    }

    public bool Remove(string name) => tags.Remove(name);

    public bool TryGet<TTag>(string name, out TTag? tag) where TTag : Tag {
        if (tags.TryGetValue(name, out Tag? value) && value is TTag typed) {
            tag = typed;
            return true;
        }

        tag = null;
        return false;
    }

    public TTag? Get<TTag>(string name) where TTag : Tag => TryGet<TTag>(name, out TTag? tag) ? tag : null;

    internal override void Read(ref BinaryReader reader, Options options) {
        tags.Clear();
        while (true) {
            TagType type = Nbt.ReadTagType(ref reader);
            if (type == TagType.End)
                return;

            string name = options.HasName ? Nbt.ReadString(ref reader, options) : string.Empty;
            Tag tag = Nbt.Read(ref reader, type, options);
            Add(name, tag);
        }
    }

    internal override void Write(ref BinaryWriter writer, Options options) {
        foreach ((string name, Tag tag) in tags) {
            writer.WriteUInt8((byte)tag.Type);
            if (options.HasName)
                Nbt.WriteString(ref writer, name, options);
            Nbt.Write(ref writer, tag, options);
        }

        writer.WriteUInt8((byte)TagType.End);
    }

    internal override int GetPayloadSize(Options options) {
        int size = sizeof(byte);
        foreach ((string name, Tag tag) in tags)
            size = checked(size + sizeof(byte) + (options.HasName ? Nbt.GetStringSize(name, options) : 0) + tag.GetPayloadSize(options));
        return size;
    }
}
