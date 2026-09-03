using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class MaterialReducerEntryOutput : DataType {
    public int ItemId;
    public int ItemCount;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(ItemId);
        writer.WriteZigZag(ItemCount);
    }

    public override void Read(ref BinaryReader reader) {
        ItemId = reader.ReadZigZag();
        ItemCount = reader.ReadZigZag();
    }
}
