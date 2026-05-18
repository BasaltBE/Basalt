using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class FullContainerName : DataType
{
    public byte ContainerId { get; set; }

    public uint? DynamicContainerId { get; set; }

    public void Read(BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();

        var isDynamic = reader.ReadBool();

        if (isDynamic)
        {
            DynamicContainerId = reader.ReadUInt32(true);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        // DynamicContainerId.Write(writer, static (BinaryWriter w, uint value) => w.WriteUInt32(value, true));

        if (DynamicContainerId.HasValue)
        {
            // Has Dinamic value bool
            writer.WriteBool(true);
            // The dinamic value itself
            writer.WriteUInt32(DynamicContainerId.Value, true);

        }
        else
        {
            // no value
            writer.WriteBool(false);
        }
    }
}
