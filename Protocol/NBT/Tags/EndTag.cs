using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

[Tag(TagType.End)]
public sealed class EndTag : BaseTag
{
    public override object? ToJsonValue() => null;

    public override void Write(BinaryWriter writer, TagOptions options)
    {
        if (options.Name)
            WriteString(writer, Name ?? string.Empty, options.VarInt);
    }

    public static EndTag Read(BinaryReader reader, TagOptions options = default) =>
        new()
        {
            Name = options.Name ? ReadString(reader, options.VarInt) : null
        };
}
