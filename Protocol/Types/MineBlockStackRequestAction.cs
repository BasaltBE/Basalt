using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class MineBlockStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 11;
    public int HotbarSlot { get; set; }
    public int PredictedDurability { get; set; }
    public int StackNetworkId { get; set; }

    public void Read(ref BinaryReader reader)
    {
        HotbarSlot = reader.ReadZigZag();
        PredictedDurability = reader.ReadZigZag();
        StackNetworkId = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(HotbarSlot);
        writer.WriteZigZag(PredictedDurability);
        writer.WriteZigZag(StackNetworkId);
    }
}
