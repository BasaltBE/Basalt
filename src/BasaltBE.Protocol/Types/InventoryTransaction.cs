using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class InventoryTransaction : DataType {
    public InventoryAction[] Actions = [];

    public override void Read(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count > 100) throw new InvalidDataException("Inventory transaction cannot contain more than 100 actions.");
        Actions = new InventoryAction[(int)count];
        for (int i = 0; i < Actions.Length; i++) {
            Actions[i] = new InventoryAction();
            Actions[i].Read(ref reader);
        }
    }

    public override void Write(ref BinaryWriter writer) {
        if (Actions.Length > 100) throw new InvalidDataException("Inventory transaction cannot contain more than 100 actions.");
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (InventoryAction action in Actions) action.Write(ref writer);
    }
}
