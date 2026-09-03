using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.NBT;

[Tag(TagType.String)]
public sealed class StringTag : BaseTag {
    public string Value { get; set; } = string.Empty;
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        WriteString(writer, Value, options.VarInt);
    }

    public static StringTag Read(BinaryReader reader, TagOptions options = default) =>
        new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = ReadString(reader, options.VarInt)
        };
}
