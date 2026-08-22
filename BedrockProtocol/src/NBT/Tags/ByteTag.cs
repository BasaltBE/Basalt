using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.NBT;
[Tag(TagType.Byte)]
public sealed class ByteTag : BaseTag {
    public sbyte Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options) {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        writer.WriteInt8(Value);
    }

    public static ByteTag Read(BinaryReader reader, TagOptions options = default) =>
        new() {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = reader.ReadInt8()
        };
}
