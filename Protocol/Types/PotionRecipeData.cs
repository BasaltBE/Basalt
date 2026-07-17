using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PotionRecipeData : DataType
{
    public int InputPotionId;
    public int InputPotionMetadata;
    public int ReagentItemId;
    public int ReagentItemMetadata;
    public int OutputPotionId;
    public int OutputPotionMetadata;

    public void Read(BinaryReader reader)
    {
        InputPotionId = reader.ReadZigZag();
        InputPotionMetadata = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        ReagentItemMetadata = reader.ReadZigZag();
        OutputPotionId = reader.ReadZigZag();
        OutputPotionMetadata = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(InputPotionId);
        writer.WriteZigZag(InputPotionMetadata);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(ReagentItemMetadata);
        writer.WriteZigZag(OutputPotionId);
        writer.WriteZigZag(OutputPotionMetadata);
    }
}
