using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Nbt;

[Tag(TagType.Long)]
public sealed class LongTag : BaseTag {
    public long Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        if (options.VarInt)
            writer.WriteZigZong(Value);
        else
            writer.WriteInt64(Value, true);
    }

    public static LongTag Read(BinaryReader reader, TagOptions options = default) =>
        new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = options.VarInt ? reader.ReadZigZong() : reader.ReadInt64(true)
        };
}
