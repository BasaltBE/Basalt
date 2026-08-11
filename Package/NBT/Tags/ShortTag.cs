using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Nbt;

[Tag(TagType.Short)]
public sealed class ShortTag : BaseTag {
    public short Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        writer.WriteInt16(Value, true);
    }

    public static ShortTag Read(BinaryReader reader, TagOptions options = default) =>
        new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = reader.ReadInt16(true)
        };
}
