using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class FloatTag : BaseTag
{
    public override TagType Type => TagType.Float;
    public float Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        writer.WriteF32(Value, true);
    }

    public static FloatTag Read(ref BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(ref reader, effective.VarInt) : null;
        return new FloatTag
        {
            Name = name,
            Value = reader.ReadF32(true)
        };
    }
}


