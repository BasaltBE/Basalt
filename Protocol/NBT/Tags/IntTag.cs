using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.Int)]
public sealed class IntTag : BaseTag
{
    public int Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options)
    {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        if (options.VarInt)
            writer.WriteZigZag(Value);
        else
            writer.WriteInt32(Value, true);
    }

    public static IntTag Read(BinaryReader reader, TagOptions options = default) =>
        new()
        {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = options.VarInt ? reader.ReadZigZag() : reader.ReadInt32(true)
        };
}
