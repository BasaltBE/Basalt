using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ActorDataList : DataType {
    public ActorDataItem[] Items = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Items.Length);
        for (int i = 0; i < Items.Length; i++) Items[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Items = new ActorDataItem[count];
        for (int i = 0; i < count; i++) {
            Items[i] = new ActorDataItem();
            Items[i].Read(ref reader);
        }
    }
}
