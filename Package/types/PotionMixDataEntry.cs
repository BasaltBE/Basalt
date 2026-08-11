using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PotionMixDataEntry {
    public int FromPotionId;
    public int FromItemAux;
    public int ReagentItemId;
    public int ReagentItemAux;
    public int ToPotionId;
    public int ToItemAux;

    public void Read(BinaryReader reader) {
        FromPotionId = reader.ReadZigZag();
        FromItemAux = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        ReagentItemAux = reader.ReadZigZag();
        ToPotionId = reader.ReadZigZag();
        ToItemAux = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(FromPotionId);
        writer.WriteZigZag(FromItemAux);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(ReagentItemAux);
        writer.WriteZigZag(ToPotionId);
        writer.WriteZigZag(ToItemAux);
    }
}
