using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ContainerMixDataEntry : DataType {
    public int FromItemId;
    public int ReagentItemId;
    public int ToItemId;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(FromItemId);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(ToItemId);
    }

    public override void Read(ref BinaryReader reader) {
        FromItemId = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        ToItemId = reader.ReadZigZag();
    }
}
