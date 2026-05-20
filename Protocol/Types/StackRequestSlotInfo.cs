using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackRequestSlotInfo : DataType
{
    public FullContainerName Container { get; set; } = new();
    public byte Slot { get; set; }
    public int StackNetworkId { get; set; }

    public void Read(BinaryReader reader)
    {
        Container.Read(reader);
        Slot = reader.ReadUInt8();
        StackNetworkId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        Container.Write(writer);
        writer.WriteUInt8(Slot);
        writer.WriteZigZag(StackNetworkId);
    }
}
