using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class StringTag : BaseTag
{
    public override TagType Type => TagType.String;
    public string Value { get; set; } = string.Empty;
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }

        WriteString(writer, Value, options.VarInt);
    }

    public static StringTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null;
        return new StringTag
        {
            Name = name,
            Value = ReadString(reader, effective.VarInt)
        };
    }
}


