using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class MineBlockStackRequestAction : IStackRequestAction, DataType {
    public byte ActionType => 11;
    /// <summary>
    /// Hotbar slot used while mining.
    /// </summary>
    public int HotbarSlot;
    /// <summary>
    /// Predicted durability after mining.
    /// </summary>
    public int PredictedDurability;
    /// <summary>
    /// Client stack network id for validation.
    /// </summary>
    public int StackNetworkId;
    public void Read(BinaryReader reader) {
        HotbarSlot = reader.ReadZigZag();
        PredictedDurability = reader.ReadZigZag();
        StackNetworkId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(HotbarSlot);
        writer.WriteZigZag(PredictedDurability);
        writer.WriteZigZag(StackNetworkId);
    }
}
