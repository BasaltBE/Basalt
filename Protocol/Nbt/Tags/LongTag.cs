using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class LongTag : BaseTag
{
    public override TagType Type => TagType.Long;
    public long Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(ref BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(ref writer, Name, options.VarInt);
        }

        writer.WriteInt64(Value, true);
    }

    public static LongTag Read(ref BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(ref reader, effective.VarInt) : null;
        return new LongTag
        {
            Name = name,
            Value = reader.ReadInt64(true)
        };
    }
}


