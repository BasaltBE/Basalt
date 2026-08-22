using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class InventoryTransactionData : DataType {
    public InventoryActionData[] Actions = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (InventoryActionData action in Actions) action.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Actions = new InventoryActionData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Actions.Length; index++) {
            InventoryActionData action = new();
            action.Read(ref reader);
            Actions[index] = action;
        }
    }
}
