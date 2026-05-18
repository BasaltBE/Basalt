using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Nbt;

public sealed class EndTag : BaseTag
{
    public override TagType Type => TagType.End;
    public override object? ToJsonValue() => null;

    public override void Write(BinaryWriter writer, ReadWriteOptions options, bool canHaveName = true)
    {
        if (canHaveName && options.Name)
        {
            WriteName(writer, Name, options.VarInt);
        }
    }

    public static EndTag Read(BinaryReader reader, ReadWriteOptions options = default, bool canHaveName = true)
    {
        ReadWriteOptions effective = options == default ? new ReadWriteOptions() : options;
        return new EndTag
        {
            Name = canHaveName && effective.Name ? ReadName(reader, effective.VarInt) : null
        };
    }
}


