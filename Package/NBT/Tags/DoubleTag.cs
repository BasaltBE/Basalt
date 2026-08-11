using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Nbt;

[Tag(TagType.Double)]
public sealed class DoubleTag : BaseTag {
    public double Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        writer.WriteF64(Value, true);
    }

    public static DoubleTag Read(BinaryReader reader, TagOptions options = default) =>
        new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = reader.ReadF64(true)
        };
}
