using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PotionMixDataEntry : DataType {
    public int FromPotionId;
    public int FromItemAux;
    public int ReagentItemId;
    public int ReagentItemAux;
    public int ToPotionId;
    public int ToItemAux;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(FromPotionId);
        writer.WriteZigZag(FromItemAux);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(ReagentItemAux);
        writer.WriteZigZag(ToPotionId);
        writer.WriteZigZag(ToItemAux);
    }

    public override void Read(ref BinaryReader reader) {
        FromPotionId = reader.ReadZigZag();
        FromItemAux = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        ReagentItemAux = reader.ReadZigZag();
        ToPotionId = reader.ReadZigZag();
        ToItemAux = reader.ReadZigZag();
    }
}
