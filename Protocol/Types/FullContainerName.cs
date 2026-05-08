using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class FullContainerName : DataType
{
    public byte ContainerId { get; set; }
    public OptionalValue<uint> DynamicContainerId { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();
        DynamicContainerId.Read(ref reader, static (ref BinaryReader r) => r.ReadUInt32(true));
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        DynamicContainerId.Write(ref writer, static (ref BinaryWriter w, uint value) => w.WriteUInt32(value, true));
    }
}
