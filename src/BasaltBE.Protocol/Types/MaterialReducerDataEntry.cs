using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class MaterialReducerDataEntry : DataType {
    public int FromItemKey;
    public MaterialReducerEntryOutput[] Outputs = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(FromItemKey);
        writer.WriteVarUInt((uint)Outputs.Length);
        foreach (MaterialReducerEntryOutput output in Outputs)
            output.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        FromItemKey = reader.ReadZigZag();
        int count = checked((int)reader.ReadVarUInt());
        Outputs = new MaterialReducerEntryOutput[count];
        for (int i = 0; i < count; i++) {
            Outputs[i] = new MaterialReducerEntryOutput();
            Outputs[i].Read(ref reader);
        }
    }
}
