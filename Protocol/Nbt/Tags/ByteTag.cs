using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class ByteTag : BaseTag
{
    public override TagType Type => TagType.Byte;
    public sbyte Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        Value = reader.ReadInt8();
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        writer.WriteInt8(Value);
    }
}
