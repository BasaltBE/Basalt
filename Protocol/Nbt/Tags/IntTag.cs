using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class IntTag : BaseTag
{
    public override TagType Type => TagType.Int;
    public int Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Read(ref BinaryReader reader, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            Name = ReadName(ref reader, options.VarInt);
        }

        Value = reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        writer.WriteInt32(Value, true);
    }
}
