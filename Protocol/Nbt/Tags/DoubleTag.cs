using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class DoubleTag : BaseTag
{
    public override TagType Type => TagType.Double;
    public double Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }

        writer.WriteF64(Value, true);
    }

    public static DoubleTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null;
        return new DoubleTag
        {
            Name = name,
            Value = reader.ReadF64(true)
        };
    }
}


