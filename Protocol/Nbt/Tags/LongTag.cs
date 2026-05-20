using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class LongTag : BaseTag
{
    public override TagType Type => TagType.Long;
    public long Value { get; set; }
    public override object ToJsonValue() => Value;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }

        if (options.VarInt)
        {
            writer.WriteZigZong(Value);
        }
        else
        {
            writer.WriteInt64(Value, true);
        }
    }

    public static LongTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        string? name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null;
        return new LongTag
        {
            Name = name,
            Value = effective.VarInt ? reader.ReadZigZong() : reader.ReadInt64(true)
        };
    }
}


