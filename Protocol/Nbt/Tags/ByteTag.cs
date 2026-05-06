using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class ByteTag : BaseTag
{
    public override TagType Type => TagType.Byte;
    public sbyte Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        writer.WriteInt8(Value);
    }

    public static ByteTag Read(ref BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(ref reader, effective.VarInt) : null;
        return new ByteTag
        {
            Name = name,
            Value = reader.ReadInt8()
        };
    }
}


