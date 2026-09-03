using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class MaterialReducerDataEntry : DataType {
    public int FromItemKey;
    public MaterialReducerEntryOutput[] Outputs = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(FromItemKey);
        writer.WriteVarUInt((uint)Outputs.Length);
        for (int i = 0; i < Outputs.Length; i++) Outputs[i].Write(ref writer);
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
