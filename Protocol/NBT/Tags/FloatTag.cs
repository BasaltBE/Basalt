using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.Float)]
public sealed class FloatTag : BaseTag
{
    public float Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, TagOptions options)
    {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);

        writer.WriteF32(Value, true);
    }

    public static FloatTag Read(BinaryReader reader, TagOptions options = default) =>
        new()
        {
            Name = options.Name ? ReadString(reader, options.VarInt) : null,
            Value = reader.ReadF32(true)
        };
}
